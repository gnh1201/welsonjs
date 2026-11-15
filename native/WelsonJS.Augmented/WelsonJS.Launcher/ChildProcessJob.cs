// ChildProcessJob.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// A Windows job object that ends the launcher's child processes when the launcher ends,
    /// however it ends (closed, killed, stopped in the debugger, or crashed): the job is set
    /// to JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE, and its only handle is held by this process, so
    /// Windows closes it and ends every process in the job when this process exits. Processes
    /// that a child starts join the job as well (for example the model instances of the
    /// llama.cpp router, and the programs run in a terminal session).
    ///
    /// The normal shutdown (ResourceServer.ShutdownChildProcesses) still stops the children
    /// first; the job covers the exits that skip it.
    /// </summary>
    public static class ChildProcessJob
    {
        private const int JobObjectExtendedLimitInformation = 9;
        private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

        private static readonly object SyncRoot = new object();
        private static IntPtr _job;

        /// <summary>
        /// Adds a started process to the job. Throws when the job cannot be created or the
        /// process cannot be added (the caller decides whether that is fatal).
        /// </summary>
        public static void Add(IntPtr processHandle)
        {
            if (!AssignProcessToJobObject(GetJob(), processHandle))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "AssignProcessToJobObject failed.");
        }

        public static void Add(Process process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            Add(process.Handle);
        }

        // Created once and never closed: closing the handle ends the processes in the job.
        private static IntPtr GetJob()
        {
            lock (SyncRoot)
            {
                if (_job != IntPtr.Zero) return _job;

                IntPtr job = CreateJobObject(IntPtr.Zero, null);
                if (job == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateJobObject failed.");

                var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
                info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
                int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
                IntPtr buffer = Marshal.AllocHGlobal(length);
                try
                {
                    Marshal.StructureToPtr(info, buffer, false);
                    if (!SetInformationJobObject(job, JobObjectExtendedLimitInformation, buffer, (uint)length))
                    {
                        int error = Marshal.GetLastWin32Error();
                        CloseHandle(job);
                        throw new Win32Exception(error, "SetInformationJobObject failed.");
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }

                _job = job;
                return _job;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateJobObjectW")]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(IntPtr hJob, int jobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
