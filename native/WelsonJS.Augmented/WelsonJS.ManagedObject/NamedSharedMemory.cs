// NamedSharedMemory.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace WelsonJS.ManagedObject
{
    /// <summary>
    /// Text exchange between processes through a named, pagefile-backed
    /// file mapping (Access to a shared memory #96).
    /// </summary>
    /// <remarks>
    /// - The region is <see cref="Capacity"/> bytes. Text is stored as UTF-8
    ///   followed by a NUL terminator, so at most Capacity - 1 bytes of text
    ///   fit. ASCII text is byte-compatible with the previous ANSI format;
    ///   data that is not valid UTF-8 is decoded with the system ANSI code
    ///   page for compatibility with older writers.
    /// - Mappings are shared per process by name and stay open until
    ///   <see cref="CloseSharedMemory"/> is called or the process exits.
    ///   The OS destroys a mapping once every process has closed it.
    /// - Reads and writes are not synchronized across processes; a reader
    ///   may observe a partially written value.
    /// - Names prefixed with "Global\" require SeCreateGlobalPrivilege.
    /// </remarks>
    [ComVisible(true)]
    [Guid("197fd5a2-f4cb-4da5-8c05-003af563614a")]
    [ProgId("WelsonJS.NamedSharedMemory")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class NamedSharedMemory
    {
        /// <summary>Size of the shared region in bytes, including the NUL terminator.</summary>
        public const int Capacity = 1024;

        private static readonly Dictionary<string, Mapping> Mappings =
            new Dictionary<string, Mapping>(StringComparer.Ordinal);

        private static readonly object SyncRoot = new object();

        private static readonly Encoding StrictUtf8 =
            new UTF8Encoding(false, true);

        private string _lastError = string.Empty;

        private sealed class Mapping
        {
            public IntPtr Handle; // file mapping object
            public IntPtr View;   // mapped view base address
        }

        private enum FileProtection : uint
        {
            PAGE_READWRITE = 0x04u
        }

        [Flags]
        private enum FileMapAccess : uint
        {
            FILE_MAP_WRITE = 0x0002u,
            FILE_MAP_READ = 0x0004u,
            FILE_MAP_ALL_ACCESS = 0x000F001Fu
        }

        private static class NativeMethods
        {
            public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpAttributes, FileProtection flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, FileMapAccess dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, UIntPtr dwNumberOfBytesToMap);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr hObject);
        }

        // Required for COM activation (CreateObject("WelsonJS.NamedSharedMemory")).
        public NamedSharedMemory()
        {
        }

        // Kept for compatibility: opens (or creates) the named mapping eagerly.
        public NamedSharedMemory(string lpName)
        {
            Mapping mapping;
            TryGetMapping(lpName, out mapping);
        }

        /// <summary>Description of the last failure on this instance, or an empty string.</summary>
        public string LastError
        {
            get { return _lastError; }
        }

        public bool WriteTextToSharedMemory(string lpName, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
            if (bytes.Length > Capacity - 1)
            {
                return Fail("The text is " + bytes.Length + " bytes in UTF-8, but shared memory \"" +
                    lpName + "\" holds at most " + (Capacity - 1) + " bytes.");
            }

            lock (SyncRoot)
            {
                Mapping mapping;
                if (!TryGetMapping(lpName, out mapping))
                    return false;

                // Zero the whole region so that no stale bytes follow the terminator.
                byte[] buffer = new byte[Capacity];
                Buffer.BlockCopy(bytes, 0, buffer, 0, bytes.Length);
                Marshal.Copy(buffer, 0, mapping.View, Capacity);
            }

            return Succeed();
        }

        public string ReadTextFromSharedMemory(string lpName)
        {
            byte[] buffer = new byte[Capacity];

            lock (SyncRoot)
            {
                Mapping mapping;
                if (!TryGetMapping(lpName, out mapping))
                    return string.Empty;

                Marshal.Copy(mapping.View, buffer, 0, Capacity);
            }

            int length = Array.IndexOf(buffer, (byte)0);
            if (length < 0)
                length = Capacity;

            Succeed();
            return Decode(buffer, length);
        }

        public bool ClearSharedMemory(string lpName)
        {
            lock (SyncRoot)
            {
                Mapping mapping;
                if (!TryGetMapping(lpName, out mapping))
                    return false;

                Marshal.Copy(new byte[Capacity], 0, mapping.View, Capacity);
            }

            return Succeed();
        }

        public bool CloseSharedMemory(string lpName)
        {
            if (!ValidateName(lpName))
                return false;

            Mapping mapping;
            lock (SyncRoot)
            {
                if (!Mappings.TryGetValue(lpName, out mapping))
                    return Fail("Shared memory \"" + lpName + "\" is not open in this process.");

                // Remove first so later calls reopen the mapping instead of
                // touching an unmapped view.
                Mappings.Remove(lpName);
                Release(mapping);
            }

            return Succeed();
        }

        private bool TryGetMapping(string lpName, out Mapping mapping)
        {
            mapping = null;

            if (!ValidateName(lpName))
                return false;

            lock (SyncRoot)
            {
                if (Mappings.TryGetValue(lpName, out mapping))
                    return true;

                // Opens the mapping if another process already created it.
                IntPtr handle = NativeMethods.CreateFileMapping(
                    NativeMethods.INVALID_HANDLE_VALUE,
                    IntPtr.Zero,
                    FileProtection.PAGE_READWRITE,
                    0u,
                    (uint)Capacity,
                    lpName
                );
                if (handle == IntPtr.Zero)
                    return FailWin32("CreateFileMapping", lpName);

                IntPtr view = NativeMethods.MapViewOfFile(
                    handle,
                    FileMapAccess.FILE_MAP_READ | FileMapAccess.FILE_MAP_WRITE,
                    0u,
                    0u,
                    new UIntPtr((uint)Capacity)
                );
                if (view == IntPtr.Zero)
                {
                    bool result = FailWin32("MapViewOfFile", lpName);
                    NativeMethods.CloseHandle(handle);
                    return result;
                }

                mapping = new Mapping { Handle = handle, View = view };
                Mappings.Add(lpName, mapping);
                return true;
            }
        }

        private static void Release(Mapping mapping)
        {
            if (mapping.View != IntPtr.Zero)
            {
                NativeMethods.UnmapViewOfFile(mapping.View);
                mapping.View = IntPtr.Zero;
            }

            if (mapping.Handle != IntPtr.Zero)
            {
                NativeMethods.CloseHandle(mapping.Handle);
                mapping.Handle = IntPtr.Zero;
            }
        }

        private static string Decode(byte[] buffer, int length)
        {
            try
            {
                return StrictUtf8.GetString(buffer, 0, length);
            }
            catch (DecoderFallbackException)
            {
                // Written by an older ANSI-based writer.
                return Encoding.Default.GetString(buffer, 0, length);
            }
        }

        private bool ValidateName(string lpName)
        {
            if (string.IsNullOrEmpty(lpName))
                return Fail("A shared memory name is required.");

            return true;
        }

        private bool FailWin32(string function, string lpName)
        {
            int error = Marshal.GetLastWin32Error();
            return Fail(function + " failed for shared memory \"" + lpName + "\": " +
                new Win32Exception(error).Message + " (" + error + ")");
        }

        private bool Fail(string message)
        {
            _lastError = message;
            return false;
        }

        private bool Succeed()
        {
            _lastError = string.Empty;
            return true;
        }
    }
}
