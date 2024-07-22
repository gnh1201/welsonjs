/*
 * WelsonJS.Toolkit: WelsonJS native component
 * 
 *     filename:
 *         NamedSharedMemory.cs
 * 
 *     description:
 *         WelsonJS - Build a Windows app on the Windows built-in JavaScript engine
 * 
 *     website:
 *         - https://github.com/gnh1201/welsonjs
 *         - https://catswords.social/@catswords_oss
 *         - https://teams.live.com/l/community/FEACHncAhq8ldnojAI
 * 
 *     author:
 *         Namhyeon Go <abuse@catswords.net>
 *
 *     license:
 *         GPLv3 or MS-RL(Microsoft Reciprocal License)
 * 
 */
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WelsonJS
{
    public class NamedSharedMemory
    {
        private IntPtr hFile;
        private IntPtr hFileMappingObject;
        private string lpName;
        private static Dictionary<string, NamedSharedMemory> memoryMap = new Dictionary<string, NamedSharedMemory>();

        [Flags]
        public enum FileProtection : uint
        {
            PAGE_NOACCESS = 1u,
            PAGE_READONLY = 2u,
            PAGE_READWRITE = 4u,
            PAGE_WRITECOPY = 8u,
            PAGE_EXECUTE = 0x10u,
            PAGE_EXECUTE_READ = 0x20u,
            PAGE_EXECUTE_READWRITE = 0x40u,
            PAGE_EXECUTE_WRITECOPY = 0x80u,
            PAGE_GUARD = 0x100u,
            PAGE_NOCACHE = 0x200u,
            PAGE_WRITECOMBINE = 0x400u,
            SEC_FILE = 0x800000u,
            SEC_IMAGE = 0x1000000u,
            SEC_RESERVE = 0x4000000u,
            SEC_COMMIT = 0x8000000u,
            SEC_NOCACHE = 0x10000000u
        }

        [Flags]
        public enum FileMapAccess
        {
            FILE_MAP_COPY = 1,
            FILE_MAP_WRITE = 2,
            FILE_MAP_READ = 4,
            FILE_MAP_ALL_ACCESS = 0xF001F
        }

        public class FileMappingNative
        {
            public const int INVALID_HANDLE_VALUE = -1;

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr CreateFileMapping(IntPtr hFile, IntPtr lpAttributes, FileProtection flProtect, uint dwMaximumSizeHigh, uint dwMaximumSizeLow, string lpName);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, FileMapAccess dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr OpenFileMapping(FileMapAccess dwDesiredAccess, bool bInheritHandle, string lpName);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr hHandle);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern uint GetLastError();
        }

        public NamedSharedMemory(string lpName)
        {
            this.lpName = lpName;
            Open();
        }

        public bool Open()
        {
            if (memoryMap.ContainsKey(lpName))
            {
                hFile = memoryMap[lpName].hFile;
                hFileMappingObject = memoryMap[lpName].hFileMappingObject;
                return true;
            }

            try
            {
                hFile = FileMappingNative.CreateFileMapping((IntPtr)(-1), IntPtr.Zero, FileProtection.PAGE_READWRITE, 0u, 1024u, lpName);
                hFileMappingObject = FileMappingNative.MapViewOfFile(hFile, FileMapAccess.FILE_MAP_ALL_ACCESS, 0u, 0u, 1024u);
                memoryMap.Add(lpName, this);
            }
            catch
            {
                return false;
            }

            return IsInitialized();
        }

        public bool IsInitialized()
        {
            return hFile != IntPtr.Zero;
        }

        public string ReadText()
        {
            try 
            {
                if (hFile == IntPtr.Zero || hFileMappingObject == IntPtr.Zero)
                {
                    throw new Exception("Could not access the shared memory");
                }
                return Marshal.PtrToStringAnsi(hFileMappingObject);
            }
            catch (Exception e)
            {
                return "Exception: " + e.Message;
            }
        }

        public bool WriteText(string text, int size = 1024)
        {
            try
            {
                if (hFile == IntPtr.Zero || hFileMappingObject == IntPtr.Zero)
                {
                    throw new Exception("Could not access the shared memory");
                }
                byte[] bytes = Encoding.ASCII.GetBytes(text);
                byte[] array = new byte[size + 1];
                Array.Copy(bytes, array, bytes.Length);
                Marshal.Copy(array, 0, hFileMappingObject, size);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool Clear(int size = 1024)
        {
            try
            {
                Marshal.Copy(new byte[size + 1], 0, hFileMappingObject, size);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool Close()
        {
            try
            {
                if (hFile == IntPtr.Zero || hFileMappingObject == IntPtr.Zero)
                {
                    throw new Exception("Could not access the shared memory");
                }

                FileMappingNative.UnmapViewOfFile(hFileMappingObject);
                hFileMappingObject = IntPtr.Zero;
                FileMappingNative.CloseHandle(hFile);
                hFile = IntPtr.Zero;
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
