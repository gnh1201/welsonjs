// NativeBootstrap.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace WelsonJS.Launcher
{
    public static class NativeBootstrap
    {
        // Win32 APIs
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool SetDefaultDllDirectories(int DirectoryFlags);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "AddDllDirectory")]
        private static extern IntPtr AddDllDirectory(string newDirectory);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetDllDirectory(string lpPathName);

        private const int LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;

        /// <summary>
        /// Tries to load native libraries in the following order:
        ///   1) %APPDATA%\{appDataSubdirectory}\{dllName}
        ///   2) Application base directory\{dllName}
        ///
        /// Must be called before any P/Invoke usage.
        /// </summary>
        public static void Init(IEnumerable<string> dllNames, string appDataSubdirectory, ICompatibleLogger logger)
        {
            if (dllNames == null) throw new ArgumentNullException(nameof(dllNames));
            if (logger == null) throw new ArgumentNullException(nameof(logger));

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDataPath = string.IsNullOrEmpty(appDataSubdirectory)
                ? appData
                : Path.Combine(appData, appDataSubdirectory);

            string asmLocation = Assembly.GetEntryAssembly()?.Location
                                 ?? Assembly.GetExecutingAssembly().Location
                                 ?? AppContext.BaseDirectory;
            string appBaseDirectory = Path.GetDirectoryName(asmLocation) ?? AppContext.BaseDirectory;

            var triedPaths = new List<string>();

            foreach (string dllName in dllNames)
            {
                if (string.IsNullOrWhiteSpace(dllName))
                    continue;

                // 1) %APPDATA% subdirectory
                string candidate1 = Path.Combine(appDataPath, dllName);
                triedPaths.Add(candidate1);
                if (TryLoad(candidate1, logger)) return;

                // 2) Application base directory
                string candidate2 = Path.Combine(appBaseDirectory, dllName);
                triedPaths.Add(candidate2);
                if (TryLoad(candidate2, logger)) return;
            }

            string message = "Failed to load requested native libraries.\n" +
                             "Tried:\n  " + string.Join("\n  ", triedPaths);
            logger.Error(message);
            throw new FileNotFoundException(message);
        }

        private static bool TryLoad(string fullPath, ICompatibleLogger logger)
        {
            try
            {
                if (!File.Exists(fullPath))
                {
                    logger.Info($"Not found: {fullPath}");
                    return false;
                }

                string directoryPath = Path.GetDirectoryName(fullPath) ?? AppContext.BaseDirectory;
                if (!TryRegisterSearchDirectory(directoryPath, logger))
                {
                    logger.Warn($"Could not register search directory: {directoryPath}");
                }

                logger.Info($"Loading: {fullPath}");
                IntPtr handle = LoadLibrary(fullPath);
                if (handle == IntPtr.Zero)
                {
                    int err = Marshal.GetLastWin32Error();
                    logger.Warn($"LoadLibrary failed for {fullPath} (Win32Error={err}).");
                    return false;
                }

                logger.Info($"Successfully loaded: {fullPath}");
                return true;
            }
            catch (Exception ex)
            {
                logger.Warn($"Exception while loading {fullPath}: {ex.Message}");
                return false;
            }
        }

        private static bool TryRegisterSearchDirectory(string directoryPath, ICompatibleLogger logger)
        {
            try
            {
                bool ok = SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS);
                if (!ok)
                {
                    int e = Marshal.GetLastWin32Error();
                    logger.Warn($"SetDefaultDllDirectories failed (Win32Error={e}), fallback to SetDllDirectory.");
                    return SetDllDirectory(directoryPath);
                }

                IntPtr cookie = AddDllDirectory(directoryPath);
                if (cookie == IntPtr.Zero)
                {
                    int e = Marshal.GetLastWin32Error();
                    logger.Warn($"AddDllDirectory failed (Win32Error={e}), fallback to SetDllDirectory.");
                    return SetDllDirectory(directoryPath);
                }

                logger.Info($"Registered native DLL search directory: {directoryPath}");
                return true;
            }
            catch (EntryPointNotFoundException)
            {
                logger.Warn("DefaultDllDirectories API not available. Using SetDllDirectory fallback.");
                return SetDllDirectory(directoryPath);
            }
            catch (Exception ex)
            {
                logger.Warn($"Register search directory failed for {directoryPath}: {ex.Message}");
                return false;
            }
        }
    }
}