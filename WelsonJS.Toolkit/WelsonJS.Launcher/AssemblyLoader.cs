// AssemblyLoader.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Network-aware loader for managed (.NET) and native (C/C++) binaries.
    /// - Managed assemblies resolve via AssemblyResolve
    /// - Native modules explicitly loaded via LoadNativeModules(...)
    /// - All DLLs must have valid Authenticode signatures
    /// - Cached at: %APPDATA%\WelsonJS\assembly\{Name}\{Version}\
    /// - BaseUrl must be set by Main() before calling Register()
    /// </summary>
    public static class AssemblyLoader
    {
        /// <summary>
        /// Base URL for downloading managed/native binaries.
        /// Example: https://catswords.blob.core.windows.net/welsonjs/packages
        /// Must be set before Register() or LoadNativeModules().
        /// </summary>
        public static string BaseUrl { get; set; } = null;
        public static ICompatibleLogger Logger { get; set; } = null;

        private static readonly object SyncRoot = new object();
        private static bool _registered;

        private static readonly string LoaderNamespace = typeof(AssemblyLoader).Namespace ?? "WelsonJS.Launcher";
        private static readonly HttpClient Http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(300)  // 5 minutes
        };

        // -------------------- kernel32 native loading --------------------

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetDllDirectory(string lpPathName);

        // -------------------- WinVerifyTrust (signature verification) --------------------

        private const uint ERROR_SUCCESS = 0x00000000;
        private const uint TRUST_E_NOSIGNATURE = 0x800B0100;
        //private const uint TRUST_E_EXPLICIT_DISTRUST = 0x800B0111;
        //private const uint TRUST_E_SUBJECT_NOT_TRUSTED = 0x800B0004;
        //private const uint CRYPT_E_SECURITY_SETTINGS = 0x80092026;

        private static readonly Guid WINTRUST_ACTION =
            new Guid("00aac56b-cd44-11d0-8cc2-00c04fc295ee");

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
        private static extern uint WinVerifyTrust(
            IntPtr hwnd,
            [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID,
            ref WINTRUST_DATA pWVTData);


        private enum FileSignatureStatus { Valid, NoSignature, Invalid }


        // -------------------- WinTrust Structures --------------------

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            [MarshalAs(UnmanagedType.LPWStr)] public string pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;

            public WINTRUST_FILE_INFO(string filePath)
            {
                cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_FILE_INFO));
                pcwszFilePath = filePath;
                hFile = IntPtr.Zero;
                pgKnownSubject = IntPtr.Zero;
            }
        }

        private enum WinTrustDataUIChoice : uint { None = 2 }
        private enum WinTrustDataRevocationChecks : uint { None = 0 }
        private enum WinTrustDataChoice : uint { File = 1 }
        private enum WinTrustDataStateAction : uint { Ignore = 0 }
        private enum WinTrustDataUIContext : uint { Execute = 0 }
        [Flags]
        private enum WinTrustDataProvFlags : uint
        {
            RevocationCheckNone = 0x00000010,
            DisableMD2andMD4 = 0x00002000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WINTRUST_DATA
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public WinTrustDataUIChoice dwUIChoice;
            public WinTrustDataRevocationChecks dwRevocationChecks;
            public WinTrustDataChoice dwUnionChoice;
            public IntPtr pFile;
            public WinTrustDataStateAction dwStateAction;
            public IntPtr hWVTStateData;
            public string pwszURLReference;
            public WinTrustDataProvFlags dwProvFlags;
            public WinTrustDataUIContext dwUIContext;

            public WINTRUST_DATA(IntPtr pFileInfo)
            {
                cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_DATA));
                pPolicyCallbackData = IntPtr.Zero;
                pSIPClientData = IntPtr.Zero;
                dwUIChoice = WinTrustDataUIChoice.None;
                dwRevocationChecks = WinTrustDataRevocationChecks.None;
                dwUnionChoice = WinTrustDataChoice.File;
                pFile = pFileInfo;
                dwStateAction = WinTrustDataStateAction.Ignore;
                hWVTStateData = IntPtr.Zero;
                pwszURLReference = null;
                dwProvFlags = WinTrustDataProvFlags.RevocationCheckNone |
                              WinTrustDataProvFlags.DisableMD2andMD4;
                dwUIContext = WinTrustDataUIContext.Execute;
            }
        }


        // ========================================================================
        // PUBLIC API
        // ========================================================================

        /// <summary>
        /// Registers AssemblyResolve to download and validate .NET assemblies.
        /// </summary>
        public static void Register()
        {
            lock (SyncRoot)
            {
                if (_registered)
                    return;

                if (string.IsNullOrWhiteSpace(BaseUrl))
                {
                    Logger.Error("AssemblyLoader.Register() called but BaseUrl is not set.");
                    throw new InvalidOperationException("AssemblyLoader.BaseUrl must be configured before Register().");
                }

                if (!BaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Error("AssemblyLoader.BaseUrl must use HTTPS for security.");
                    throw new InvalidOperationException("AssemblyLoader.BaseUrl must use HTTPS.");
                }

                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
                _registered = true;

                Logger.Info("AssemblyLoader: AssemblyResolve handler registered.");
            }
        }


        /// <summary>
        /// Loads native modules associated with an assembly (explicit).
        /// </summary>
        public static void LoadNativeModules(string ownerAssemblyName, Version version, IList<string> fileNames)
        {
            if (string.IsNullOrWhiteSpace(BaseUrl))
                throw new InvalidOperationException("AssemblyLoader.BaseUrl must be set before loading native modules.");

            if (ownerAssemblyName == null) throw new ArgumentNullException("ownerAssemblyName");
            if (version == null) throw new ArgumentNullException("version");
            if (fileNames == null) throw new ArgumentNullException("fileNames");

            string versionString = version.ToString();

            lock (SyncRoot)
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cacheDir = Path.Combine(appData, "WelsonJS", "assembly", ownerAssemblyName, versionString);
                Directory.CreateDirectory(cacheDir);

                try
                {
                    if (!SetDllDirectory(cacheDir))
                        Logger.Warn("SetDllDirectory failed for: {0}", cacheDir);
                }
                catch (Exception ex)
                {
                    Logger.Warn("SetDllDirectory threw exception: {0}", ex.Message);
                }

                foreach (string raw in fileNames)
                {
                    if (string.IsNullOrWhiteSpace(raw))
                        continue;

                    string fileName = raw.Trim();
                    string localPath = Path.Combine(cacheDir, fileName);

                    if (!File.Exists(localPath))
                    {
                        string url = $"{BaseUrl.TrimEnd('/')}/native/{ownerAssemblyName}/{versionString}/{fileName}";
                        DownloadFile(url, localPath);
                        Logger.Info("Downloaded native module: {0}", fileName);
                    }
                    else
                    {
                        Logger.Info("Using cached native module: {0}", localPath);
                    }

                    EnsureSignedFileOrThrow(localPath, fileName);

                    IntPtr h = LoadLibrary(localPath);
                    if (h == IntPtr.Zero)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        Logger.Error("LoadLibrary failed for {0} with error code {1}", localPath, errorCode);
                        throw new InvalidOperationException($"Failed to load native module: {fileName} (error: {errorCode})");
                    }
                    else
                    {
                        Logger.Info("Loaded native module: {0}", fileName);
                    }
                }
            }
        }


        public static void LoadNativeModules(Assembly asm, IList<string> fileNames)
        {
            if (asm == null)
                throw new ArgumentNullException(nameof(asm));
            if (fileNames == null)
                throw new ArgumentNullException(nameof(fileNames));

            AssemblyName an = asm.GetName();

            if (an == null)
                throw new InvalidOperationException("Assembly.GetName() returned null.");
            if (an.Name == null || an.Version == null)
                throw new InvalidOperationException("Assembly name or version is missing.");

            LoadNativeModules(an.Name, an.Version, fileNames);
        }


        // ========================================================================
        // ASSEMBLY RESOLVE HANDLER (MANAGED)
        // ========================================================================

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Logger.Info("AssemblyResolve: {0}", args.Name);

            AssemblyName req = new AssemblyName(args.Name);
            string simpleName = req.Name;
            if (IsFrameworkAssembly(simpleName))
                return null;

            var entry = Assembly.GetEntryAssembly();
            if (entry != null)
            {
                var entryName = entry.GetName().Name;
                if (string.Equals(simpleName, entryName, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("AssemblyResolve: skipping entry assembly {0}", simpleName);
                    return null;
                }
            }

            Version version = req.Version ?? new Version(0, 0, 0, 0);
            string versionStr = version.ToString();

            lock (SyncRoot)
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cacheDir = Path.Combine(appData, "WelsonJS", "assembly", simpleName, versionStr);
                string dllPath = Path.Combine(cacheDir, simpleName + ".dll");

                Directory.CreateDirectory(cacheDir);

                if (!File.Exists(dllPath))
                {
                    string url = $"{BaseUrl.TrimEnd('/')}/managed/{simpleName}/{versionStr}/{simpleName}.dll";
                    DownloadFile(url, dllPath);
                    Logger.Info("Downloaded managed assembly: {0}", simpleName);
                }
                else
                {
                    Logger.Info("Using cached managed assembly: {0}", dllPath);
                }

                if (!File.Exists(dllPath))
                {
                    Logger.Warn("AssemblyResolve: managed assembly not found after download attempt: {0}", simpleName);
                    return null;
                }

                EnsureSignedFileOrThrow(dllPath, simpleName);
                return Assembly.LoadFrom(dllPath);
            }
        }


        // ========================================================================
        // HELPERS
        // ========================================================================

        private static void DownloadFile(string url, string dest)
        {
            HttpResponseMessage res = null;

            try
            {
                string gzUrl = url + ".gz";
                bool isDll = url.EndsWith(".dll", StringComparison.OrdinalIgnoreCase); // *.dll.gz
                bool downloaded = false;

                if (isDll && TryDownloadGzipToFile(gzUrl, dest))
                {
                    Logger.Info("Downloaded and decompressed gzip file to: {0}", dest);
                    downloaded = true;
                }

                if (!downloaded)
                {
                    Logger.Info("Downloading file from: {0}", url);
                    res = Http.GetAsync(url).GetAwaiter().GetResult();
                    res.EnsureSuccessStatusCode();

                    using (Stream s = res.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write))
                    {
                        s.CopyTo(fs);
                    }

                    Logger.Info("Downloaded file to: {0}", dest);
                }

                if (!File.Exists(dest))
                {
                    throw new FileNotFoundException("File not found after download", dest);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error downloading {0}: {1}", url, ex.Message);
                throw;
            }
            finally
            {
                res?.Dispose();
            }
        }


        private static bool TryDownloadGzipToFile(string gzUrl, string dest)
        {
            string tempFile = dest + ".tmp";

            try
            {
                using (var res = Http.GetAsync(gzUrl).GetAwaiter().GetResult())
                {
                    if (res.StatusCode == HttpStatusCode.NotFound)
                        return false;

                    res.EnsureSuccessStatusCode();

                    using (Stream s = res.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (var gz = new GZipStream(s, CompressionMode.Decompress))
                    using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                    {
                        gz.CopyTo(fs);
                    }

                    File.Move(tempFile, dest);

                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger?.Warn("Failed to download or decompress gzipped file from {0}: {1}", gzUrl, ex.Message);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    try
                    {
                        File.Delete(tempFile);
                    }
                    catch (Exception ex)
                    {
                       Logger?.Info("Failed to delete temp file {0}: {1}", tempFile, ex.Message);
                    }
                }
            }

            return false;
        }


        private static bool IsFrameworkAssembly(string name)
        {
            return name.StartsWith("System", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) ||
                   name == "mscorlib" ||
                   name == "netstandard" ||
                   name == "WindowsBase" ||
                   name == "PresentationCore" ||
                   name == "PresentationFramework" ||
                   name.StartsWith(LoaderNamespace, StringComparison.OrdinalIgnoreCase);
        }


        private static void EnsureSignedFileOrThrow(string path, string logicalName)
        {
            if (!File.Exists(path))
            {
                Logger.Error("File does not exist for signature verification: {0}", logicalName);
                throw new FileNotFoundException("File not found for signature verification: " + logicalName, path);
            }

            FileSignatureStatus status = VerifySignature(path);

            if (status == FileSignatureStatus.Valid)
            {
                Logger.Info("Signature OK: {0}", logicalName);
                return;
            }

            if (status == FileSignatureStatus.NoSignature)
            {
                Logger.Error("BLOCKED unsigned binary: {0}", logicalName);
                throw new InvalidOperationException("Unsigned binary blocked: " + logicalName);
            }

            Logger.Error("BLOCKED invalid signature: {0}", logicalName);
            throw new InvalidOperationException("Invalid signature: " + logicalName);
        }


        private static FileSignatureStatus VerifySignature(string file)
        {
            WINTRUST_FILE_INFO fileInfo = new WINTRUST_FILE_INFO(file);
            IntPtr pFile = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)));

            Marshal.StructureToPtr(fileInfo, pFile, false);
            WINTRUST_DATA data = new WINTRUST_DATA(pFile);

            uint result = WinVerifyTrust(IntPtr.Zero, WINTRUST_ACTION, ref data);

            Marshal.FreeCoTaskMem(pFile);

            if (result == ERROR_SUCCESS) return FileSignatureStatus.Valid;
            if (result == TRUST_E_NOSIGNATURE) return FileSignatureStatus.NoSignature;

            return FileSignatureStatus.Invalid;
        }
    }
}
