// AssemblyLoader.cs (Catswords.Phantomizer)
// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: Namhyeon Go <gnh1201@catswords.re.kr>, 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Catswords.Phantomizer
{
    /// <summary>
    /// Network-aware loader for managed (.NET) and native (C/C++) binaries.
    /// - Managed assemblies resolve via AssemblyResolve
    /// - Native modules explicitly loaded via LoadNativeModules(...)
    /// - All DLLs must have valid Authenticode signatures
    /// - Cached at: %APPDATA%\Catswords\assembly\{Name}\{Version}\
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
        public static string LoaderNamespace { get; set; } = typeof(AssemblyLoader).Namespace;
        public static string AppName { get; set; } = "Catswords";
        public static string IntegrityUrl { get; set; } = null;

        private static HashSet<string> _integrityHashes = null;
        private static bool _integrityLoaded = false;
        private static bool _registered;

        private static readonly object AllowSchemesSyncRoot = new object();
        private static readonly object IntegritySyncRoot = new object();
        private static readonly object SyncRoot = new object();

        private static readonly HashSet<string> _allowSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            Uri.UriSchemeHttps
        };

        private static readonly HttpClientHandler LegacyHttpHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.None
        };
        private static readonly HttpClientHandler HttpHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        private static readonly HttpClient LegacyHttp = CreateClient(LegacyHttpHandler); // Does not send Accept-Encoding (gzip, deflate)
        private static readonly HttpClient Http = CreateClient(HttpHandler); // Sends Accept-Encoding (gzip, deflate) and auto-decompresses

        private static HttpClient CreateClient(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler, disposeHandler: false)
            {
                Timeout = TimeSpan.FromSeconds(300) // 5 minutes
            };

            return client;
        }

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

                // Fix TLS connectivity issues
                EnsureSecurityProtocols(SecurityProtocolType.Tls12);
                EnsureSecurityProtocolByName("Tls13");  // Add if available
                // EnsureSecurityProtocols(SecurityProtocolType.Tls11, SecurityProtocolType.Tls);  // Optional legacy compatibility (uncomment if needed)

                // Load integrity manifest
                try
                {
                    if (!_integrityLoaded)
                        LoadIntegrityManifest();

                    if (string.IsNullOrWhiteSpace(BaseUrl))
                        throw new InvalidOperationException("BaseUrl must be configured before Register().");

                    TryVerifyUrl(BaseUrl, out bool verified);
                    if (!verified)
                        throw new InvalidOperationException("BaseUrl verification failed.");
                }
                catch (Exception ex)
                {
                    Trace.TraceError("AssemblyLoader: failed to initialize: {0}", ex.Message);
                    throw;
                }

                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
                _registered = true;

                Trace.TraceInformation("AssemblyLoader: AssemblyResolve handler registered.");
            }
        }

        /// <summary>
        /// Loads native modules associated with an assembly (explicit).
        /// </summary>
        public static void LoadNativeModules(string ownerAssemblyName, Version version, IList<string> fileNames)
        {
            TryVerifyUrl(BaseUrl, out bool verified);
            if (!verified)
                throw new InvalidOperationException("BaseUrl verification failed.");

            if (ownerAssemblyName == null) throw new ArgumentNullException("ownerAssemblyName");
            if (version == null) throw new ArgumentNullException("version");
            if (fileNames == null) throw new ArgumentNullException("fileNames");

            string versionString = version.ToString();

            lock (SyncRoot)
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cacheDir = Path.Combine(appData, AppName, "assembly", ownerAssemblyName, versionString);
                Directory.CreateDirectory(cacheDir);

                try
                {
                    if (!SetDllDirectory(cacheDir))
                        Trace.TraceWarning("SetDllDirectory failed for: {0}", cacheDir);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("SetDllDirectory threw exception: {0}", ex.Message);
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
                        Trace.TraceInformation("Downloaded native module: {0}", fileName);
                    }
                    else
                    {
                        Trace.TraceInformation("Using cached native module: {0}", localPath);
                    }

                    EnsureIntegrityOrThrow(localPath);
                    EnsureSignedFileOrThrow(localPath, fileName);

                    IntPtr h = LoadLibrary(localPath);
                    if (h == IntPtr.Zero)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        Trace.TraceError("LoadLibrary failed for {0} with error code {1}", localPath, errorCode);
                        throw new InvalidOperationException($"Failed to load native module: {fileName} (error: {errorCode})");
                    }
                    else
                    {
                        Trace.TraceInformation("Loaded native module: {0}", fileName);
                    }
                }
            }
        }

        /// <summary>
        /// Adds an allowed URI scheme for assembly and module loading.
        /// Only HTTP and HTTPS schemes are supported. HTTPS is the default.
        /// Adding HTTP reduces security and will log a warning.
        /// </summary>
        /// <param name="scheme">The URI scheme to allow (e.g., "http" or "https"). Trailing colons are automatically removed.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="scheme"/> is null or whitespace.</exception>
        /// <exception cref="ArgumentException">Thrown when the scheme is invalid or not HTTP/HTTPS.</exception>
        /// <remarks>
        /// This method is thread-safe and can be called before Register() or LoadNativeModules().
        /// </remarks>
        public static void AddAllowedUriScheme(string scheme)
        {
            if (string.IsNullOrWhiteSpace(scheme))
                throw new ArgumentNullException(nameof(scheme));

            int colonIndex = scheme.IndexOf(':');
            if (colonIndex > -1)
                scheme = scheme.Substring(0, colonIndex);

            scheme = scheme.ToLowerInvariant();

            if (!Uri.CheckSchemeName(scheme))
                throw new ArgumentException("Invalid URI scheme name.", nameof(scheme));

            if (!scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                    !scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Only HTTPS or HTTP schemes are supported.", nameof(scheme));

            if (scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
                Trace.TraceWarning("Warning: Adding 'http' to allowed URI schemes reduces security.");

            lock (AllowSchemesSyncRoot)
            {
                _allowSchemes.Add(scheme);
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

        public static void AddIntegrityHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
                throw new ArgumentNullException(nameof(hash));

            lock (IntegritySyncRoot)
            {
                if (_integrityHashes == null)
                    _integrityHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                _integrityHashes.Add(hash.Trim().ToLower());
            }
        }

        // ========================================================================
        // ASSEMBLY RESOLVE HANDLER (MANAGED)
        // ========================================================================

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Trace.TraceInformation("AssemblyResolve: {0}", args.Name);

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
                    Trace.TraceInformation("AssemblyResolve: skipping entry assembly {0}", simpleName);
                    return null;
                }
            }

            Version version = req.Version ?? new Version(0, 0, 0, 0);
            string versionStr = version.ToString();

            lock (SyncRoot)
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string cacheDir = Path.Combine(appData, AppName, "assembly", simpleName, versionStr);
                string dllPath = Path.Combine(cacheDir, simpleName + ".dll");

                Directory.CreateDirectory(cacheDir);

                if (!File.Exists(dllPath))
                {
                    string url = $"{BaseUrl.TrimEnd('/')}/managed/{simpleName}/{versionStr}/{simpleName}.dll";
                    DownloadFile(url, dllPath);
                    Trace.TraceInformation("Downloaded managed assembly: {0}", simpleName);
                }
                else
                {
                    Trace.TraceInformation("Using cached managed assembly: {0}", dllPath);
                }

                if (!File.Exists(dllPath))
                {
                    Trace.TraceWarning("AssemblyResolve: managed assembly not found after download attempt: {0}", simpleName);
                    return null;
                }

                EnsureIntegrityOrThrow(dllPath);
                EnsureSignedFileOrThrow(dllPath, simpleName);
                return Assembly.LoadFrom(dllPath);
            }
        }


        // ========================================================================
        // HELPERS
        // ========================================================================

        private static void DownloadFile(string url, string dest)
        {
            try
            {
                string gzUrl = url + ".gz";
                bool isDll = url.EndsWith(".dll", StringComparison.OrdinalIgnoreCase); // *.dll.gz
                bool downloaded = false;

                if (isDll && TryDownloadCompressedFile(gzUrl, dest))
                {
                    Trace.TraceInformation("Downloaded and decompressed file to: {0}", dest);
                    downloaded = true;
                }

                if (!downloaded)
                {
                    Trace.TraceInformation("Downloading file from: {0}", url);

                    using (var stream = GetStreamFromUrl(url))
                    using (var fs = new FileStream(dest, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fs);
                    }

                    Trace.TraceInformation("Downloaded file to: {0}", dest);
                }

                if (!File.Exists(dest))
                {
                    throw new FileNotFoundException("File not found after download", dest);
                }
            }
            catch (HttpRequestException ex)
            {
                Trace.TraceError("Network or I/O error downloading {0}: {1}", url, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unexpected error downloading {0}: {1}", url, ex.Message);
                throw;
            }
        }


        private static bool TryDownloadCompressedFile(string gzUrl, string dest)
        {
            string tempFile = dest + ".tmp";

            try
            {
                using (var res = LegacyHttp.GetAsync(gzUrl).GetAwaiter().GetResult())
                {
                    if (res.StatusCode == HttpStatusCode.NotFound)
                    {
                        Trace.TraceInformation("No gzipped variant at {0}; falling back to uncompressed URL.", gzUrl);
                        return false;
                    }

                    res.EnsureSuccessStatusCode();

                    using (Stream s = res.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                    using (var gz = new GZipStream(s, CompressionMode.Decompress))
                    using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                    {
                        gz.CopyTo(fs);
                    }

                    if (File.Exists(dest))
                        File.Delete(dest);

                    File.Move(tempFile, dest);

                    return true;
                }
            }
            catch (HttpRequestException ex)
            {
                Trace.TraceWarning("Network or I/O error downloading compressed file from {0}: {1}", gzUrl, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                Trace.TraceError("Unexpected error downloading compressed file from {0}: {1}", gzUrl, ex.Message);
                throw;
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
                        Trace.TraceInformation("Failed to delete temporary file {0}: {1}", tempFile, ex.Message);
                    }
                }
            }
        }


        private static bool IsFrameworkAssembly(string name)
        {
            return name.StartsWith("System.", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase) ||
                   name == "mscorlib" ||
                   name == "netstandard" ||
                   name == "WindowsBase" ||
                   name == "PresentationCore" ||
                   name == "PresentationFramework" ||
                   name.StartsWith($"{LoaderNamespace}.", StringComparison.OrdinalIgnoreCase);
        }

        private static void LoadIntegrityManifest()
        {
            lock (IntegritySyncRoot)
            {
                if (_integrityLoaded)
                    return;

                if (string.IsNullOrWhiteSpace(IntegrityUrl))
                {
                    _integrityLoaded = true;
                    return; // integrity disabled
                }

                XDocument doc;

                try
                {
                    TryVerifyUrl(IntegrityUrl, out bool verified);
                    if (!verified)
                        throw new InvalidOperationException("IntegrityUrl verification failed.");

                    using (var stream = GetStreamFromUrl(IntegrityUrl))
                    {
                        doc = XDocument.Load(stream);
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("AssemblyIntegrity: failed to load manifest.\n{0}", ex.ToString());

                    Exception inner = ex.InnerException;
                    int depth = 0;
                    while (inner != null && depth < 8)
                    {
                        Trace.TraceError("AssemblyIntegrity: inner[{0}]\n{1}", depth, inner.ToString());
                        inner = inner.InnerException;
                        depth++;
                    }

                    throw new InvalidOperationException("Failed to load AssemblyIntegrity manifest.", ex);
                }

                XElement hashes = doc.Root?.Element("Hashes");
                if (hashes == null)
                {
                    Trace.TraceWarning("AssemblyIntegrity: <Hashes> not found. Integrity disabled.");
                    _integrityLoaded = true;
                    return;
                }

                foreach (var h in hashes.Elements("Hash"))
                {
                    var algorithm = h.Attribute("algorithm")?.Value?.Trim();

                    if (!string.Equals(algorithm, "SHA256", StringComparison.OrdinalIgnoreCase))
                        continue; // only SHA256 supported

                    string val = h.Attribute("value")?.Value?.Trim();
                    if (string.IsNullOrWhiteSpace(val))
                        continue;

                    AddIntegrityHash(val);
                }

                _integrityLoaded = true;
                Trace.TraceInformation("AssemblyIntegrity: loaded {0} allowed hashes.", _integrityHashes.Count);
            }
        }

        private static void EnsureSignedFileOrThrow(string path, string logicalName)
        {
            if (!File.Exists(path))
            {
                Trace.TraceError("File does not exist for signature verification: {0}", logicalName);
                throw new FileNotFoundException("File not found for signature verification: " + logicalName, path);
            }

            FileSignatureStatus status = VerifySignature(path);

            if (status == FileSignatureStatus.Valid)
            {
                Trace.TraceInformation("Signature OK: {0}", logicalName);
                return;
            }

            if (status == FileSignatureStatus.NoSignature)
            {
                Trace.TraceError("BLOCKED unsigned binary: {0}", logicalName);
                throw new InvalidOperationException("Unsigned binary blocked: " + logicalName);
            }

            Trace.TraceError("BLOCKED invalid signature: {0}", logicalName);
            throw new InvalidOperationException("Invalid signature: " + logicalName);
        }

        private static void EnsureIntegrityOrThrow(string path)
        {
            if (string.IsNullOrWhiteSpace(IntegrityUrl))
                return; // disabled

            if (_integrityHashes == null || _integrityHashes.Count == 0)
            {
                Trace.TraceWarning("AssemblyIntegrity: no hashes loaded → skipping check.");
                return;
            }

            byte[] bytes = File.ReadAllBytes(path);

            // Compute hashes
            string sha256 = ComputeHashHex(bytes, SHA256.Create());

            // Check match
            if (_integrityHashes.Contains(sha256))
            {
                Trace.TraceInformation("AssemblyIntegrity: hash OK for {0}", Path.GetFileName(path));
                return;
            }

            Trace.TraceError("AssemblyIntegrity: hash mismatch! SHA256={0}", sha256);

            // Delete corrupted file so the next run can re-download a clean copy.
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                    Trace.TraceInformation("AssemblyIntegrity: deleted corrupted file {0}", path);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("AssemblyIntegrity: failed to delete corrupted file {0}: {1}", path, ex.Message);
                }
            }

            throw new InvalidOperationException("AssemblyIntegrity check failed for: " + path);
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

        private static string ComputeHashHex(byte[] data, HashAlgorithm algorithm)
        {
            using (algorithm)
            {
                var hash = algorithm.ComputeHash(data);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static bool IsValidUriScheme(Uri uri)
        {
            if (uri == null)
                return false;

            lock (AllowSchemesSyncRoot)
            {
                return _allowSchemes.Contains(uri.Scheme);
            }
        }

        private static void TryVerifyUrl(string url, out bool verified)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                    throw new InvalidOperationException("URL is null or empty.");

                if (Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                {
                    if (!IsValidUriScheme(uri))
                        throw new InvalidOperationException(
                            $"URI scheme '{uri.Scheme}' is not allowed. Use AddAllowedUriScheme() to permit additional schemes.");
                }
                else
                {
                    throw new InvalidOperationException("Not a valid absolute URI.");
                }

                verified = true;
            }
            catch (Exception ex)
            {
                Trace.TraceError("URL verification failed for {0}: {1}", url, ex.Message);
                verified = false;
            }
        }

        // Adds protocol flags without overwriting existing ones.
        // Safe on older .NET/Windows where some enum members (e.g., Tls13) may not exist.
        private static void EnsureSecurityProtocols(params SecurityProtocolType[] protocols)
        {
            try
            {
                SecurityProtocolType original = ServicePointManager.SecurityProtocol;
                SecurityProtocolType current = original;

                foreach (var protocol in protocols)
                    current |= protocol;

                if (current != original)
                {
                    ServicePointManager.SecurityProtocol = current;
                    Trace.TraceInformation(
                        "SecurityProtocol updated: {0} -> {1}",
                        original, current
                    );
                }
                else
                {
                    Trace.TraceInformation(
                        "SecurityProtocol unchanged: {0}",
                        original
                    );
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "Failed to ensure security protocols ({0}): {1}",
                    string.Join(", ", protocols),
                    ex
                );
            }
        }

        // Adds protocol by enum name when available (e.g., "Tls13"), otherwise no-op.
        private static void EnsureSecurityProtocolByName(string protocolName)
        {
            if (string.IsNullOrEmpty(protocolName))
                return;

            try
            {
                SecurityProtocolType original = ServicePointManager.SecurityProtocol;
                SecurityProtocolType current = original;

                try
                {
                    SecurityProtocolType p =
                        (SecurityProtocolType)Enum.Parse(
                            typeof(SecurityProtocolType),
                            protocolName
                        );

                    current |= p;
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning(
                        "SecurityProtocol '{0}' not available in this runtime: {1}",
                        protocolName,
                        ex.Message
                    );
                    return;
                }

                if (current != original)
                {
                    ServicePointManager.SecurityProtocol = current;
                    Trace.TraceInformation(
                        "SecurityProtocol '{0}' enabled: {1} -> {2}",
                        protocolName,
                        original,
                        current
                    );
                }
                else
                {
                    Trace.TraceInformation(
                        "SecurityProtocol '{0}' already enabled: {1}",
                        protocolName,
                        original
                    );
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "Failed to enable SecurityProtocol '{0}': {1}",
                    protocolName,
                    ex
                );
            }
        }

        public static Stream CurlGetAsStream(string url)
        {
            Trace.TraceInformation("Trying curl.exe to get URL: {0}", url);

            // Resolve curl.exe only from the application base directory
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string curlExePath = Path.Combine(baseDir, "curl.exe");

            // Check existence of curl.exe
            if (!File.Exists(curlExePath))
                throw new FileNotFoundException("curl.exe was not found in the application directory.", curlExePath);

            // Check integrity of curl.exe
            byte[] bytes = File.ReadAllBytes(curlExePath);
            string sha256 = ComputeHashHex(bytes, SHA256.Create());
            if (_integrityHashes == null || !_integrityHashes.Contains(sha256))
                throw new InvalidOperationException("curl.exe integrity check failed.");

            // Prepare process start info
            var psi = new ProcessStartInfo
            {
                FileName = curlExePath,
                Arguments =
                    "-f -sS -L --retry 3 --retry-delay 1 " +
                    "--connect-timeout 10 --max-time 30 " +
                    "\"" + url + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var process = new Process { StartInfo = psi };
            if (!process.Start())
                throw new InvalidOperationException("Failed to start curl.exe process.");

            // Drain stderr asynchronously.
            // If any error line is received, log it immediately.
            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    Trace.TraceError("curl stderr: {0}", e.Data);
                }
            };
            process.BeginErrorReadLine();

            var memory = new MemoryStream();

            // Read stdout fully; this completes when stdout is closed (EOF)
            process.StandardOutput.BaseStream.CopyTo(memory);

            // Enforce a hard timeout so we never wait forever
            if (!process.WaitForExit(60000))
            {
                try { process.Kill(); } catch { }
                throw new TimeoutException("curl.exe did not exit within the hard timeout.");
            }

            if (process.ExitCode != 0)
                throw new InvalidOperationException("curl.exe failed with exit code " + process.ExitCode + ".");

            memory.Position = 0;
            return memory; // Caller must dispose the stream
        }

        private static T ExecuteWithFallback<T>(Func<T> primaryAction, Func<Exception, bool> shouldFallback, Func<T> fallbackAction)
        {
            try
            {
                return primaryAction();
            }
            catch (Exception ex)
            {
                if (shouldFallback != null && shouldFallback(ex))
                    return fallbackAction();

                throw;
            }
        }

        private static Stream GetStreamFromUrl(string url)
        {
            Trace.TraceInformation("Getting stream from URL: {0}", url);

            return ExecuteWithFallback(
                primaryAction: () =>
                {
                    var res = Http.GetAsync(url).GetAwaiter().GetResult();
                    res.EnsureSuccessStatusCode();
                    return res.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
                },
                shouldFallback: IsTlsHandshakeFailure,
                fallbackAction: () =>
                {
                    return CurlGetAsStream(url);
                }
            );
        }

        private static bool IsTlsHandshakeFailure(Exception ex)
        {
            bool isTlsException = ex is HttpRequestException httpEx &&
                httpEx.InnerException is WebException webEx &&
                webEx.Status == WebExceptionStatus.SecureChannelFailure;

            if (isTlsException)
            {
                Trace.TraceWarning("TLS handshake failure: {0}", ex.Message);
            }
            else
            {
                Trace.TraceInformation("HttpRequestException is not a TLS handshake failure: {0}", ex.Message);
            }

            return isTlsException;
        }
    }
}
