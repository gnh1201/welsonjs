// ConPtySession.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// Hosts a child process inside a Windows pseudo console (ConPTY).
    /// Output is a UTF-8 VT byte stream suitable for a terminal emulator such as xterm.js,
    /// and input is forwarded as UTF-8 so full-screen TUI programs (e.g. Claude Code) work.
    /// Requires Windows 10 version 1809 or later.
    /// </summary>
    public sealed class ConPtySession : IDisposable
    {
        private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
        private const uint CREATE_UNICODE_ENVIRONMENT = 0x00000400;
        private const int STARTF_USESTDHANDLES = 0x00000100;
        private static readonly IntPtr PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = (IntPtr)0x00020016;

        private readonly object _sync = new object();
        private IntPtr _pseudoConsole;
        private IntPtr _processHandle;
        private IntPtr _threadHandle;
        private FileStream _input;
        private FileStream _output;
        private RegisteredWaitHandle _exitWait;
        private ProcessWaitHandle _processWaitHandle;
        private readonly ManualResetEvent _processExited = new ManualResetEvent(false);
        private int _exitCode = -1;
        private int _exitRaised;
        private bool _disposed;

        public int ProcessId { get; private set; }
        public bool HasExited { get { return _processExited.WaitOne(0); } }
        public int ExitCode { get { return _exitCode; } }

        /// <summary>Raised on a background thread with each chunk of VT output.</summary>
        public event Action<byte[]> OutputReceived;

        /// <summary>Raised once, after the output stream is drained, with the process exit code.</summary>
        public event Action<int> Exited;

        private ConPtySession() { }

        public static ConPtySession Start(string commandLine, string workingDirectory, IDictionary<string, string> environment, short cols, short rows)
        {
            if (string.IsNullOrWhiteSpace(commandLine))
                throw new ArgumentException("Command line is required.", nameof(commandLine));
            if (!string.IsNullOrEmpty(workingDirectory) && !Directory.Exists(workingDirectory))
                throw new DirectoryNotFoundException($"Working directory not found: {workingDirectory}");

            var session = new ConPtySession();
            try
            {
                session.Create(commandLine, workingDirectory, environment, ClampSize(cols, 120), ClampSize(rows, 30));
                return session;
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        private void Create(string commandLine, string workingDirectory, IDictionary<string, string> environment, short cols, short rows)
        {
            SafeFileHandle inputRead, inputWrite, outputRead, outputWrite;
            if (!CreatePipe(out inputRead, out inputWrite, IntPtr.Zero, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "CreatePipe (input) failed.");
            if (!CreatePipe(out outputRead, out outputWrite, IntPtr.Zero, 0))
            {
                inputRead.Dispose();
                inputWrite.Dispose();
                throw new Win32Exception(Marshal.GetLastWin32Error(), "CreatePipe (output) failed.");
            }

            IntPtr attributeList = IntPtr.Zero;
            IntPtr environmentBlock = IntPtr.Zero;
            try
            {
                int hr;
                try
                {
                    hr = CreatePseudoConsole(new COORD { X = cols, Y = rows }, inputRead, outputWrite, 0, out _pseudoConsole);
                }
                catch (EntryPointNotFoundException)
                {
                    throw new PlatformNotSupportedException("ConPTY is not available. Windows 10 version 1809 or later is required.");
                }
                if (hr != 0)
                    throw new InvalidOperationException("CreatePseudoConsole failed.", Marshal.GetExceptionForHR(hr));

                IntPtr size = IntPtr.Zero;
                InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size);
                attributeList = Marshal.AllocHGlobal(size);
                if (!InitializeProcThreadAttributeList(attributeList, 1, 0, ref size))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "InitializeProcThreadAttributeList failed.");
                if (!UpdateProcThreadAttribute(attributeList, 0, PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE, _pseudoConsole, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "UpdateProcThreadAttribute failed.");

                var startupInfo = new STARTUPINFOEX();
                startupInfo.StartupInfo.cb = Marshal.SizeOf(typeof(STARTUPINFOEX));
                startupInfo.lpAttributeList = attributeList;
                // Without this, a child would inherit this process's own standard handles
                // (when it has a console or redirected stdio) instead of the pseudo console.
                startupInfo.StartupInfo.dwFlags = STARTF_USESTDHANDLES;

                environmentBlock = Marshal.StringToHGlobalUni(BuildEnvironmentBlock(environment));

                PROCESS_INFORMATION processInfo;
                // CreateProcessW may modify the command line buffer, so pass a mutable copy.
                var commandLineBuffer = new StringBuilder(commandLine);
                if (!CreateProcess(null, commandLineBuffer, IntPtr.Zero, IntPtr.Zero, false,
                    EXTENDED_STARTUPINFO_PRESENT | CREATE_UNICODE_ENVIRONMENT, environmentBlock,
                    string.IsNullOrEmpty(workingDirectory) ? null : workingDirectory,
                    ref startupInfo, out processInfo))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"CreateProcess failed: {commandLine}");
                }

                _processHandle = processInfo.hProcess;
                _threadHandle = processInfo.hThread;
                ProcessId = processInfo.dwProcessId;
            }
            finally
            {
                if (attributeList != IntPtr.Zero)
                {
                    DeleteProcThreadAttributeList(attributeList);
                    Marshal.FreeHGlobal(attributeList);
                }
                if (environmentBlock != IntPtr.Zero)
                    Marshal.FreeHGlobal(environmentBlock);

                // The pseudo console owns duplicates of these ends.
                inputRead.Dispose();
                outputWrite.Dispose();
            }

            _input = new FileStream(inputWrite, FileAccess.Write, 1, false);
            _output = new FileStream(outputRead, FileAccess.Read, 1, false);

            _processWaitHandle = new ProcessWaitHandle(_processHandle);
            _exitWait = ThreadPool.RegisterWaitForSingleObject(_processWaitHandle, (state, timedOut) => OnProcessExited(), null, Timeout.Infinite, true);

            var reader = new Thread(ReadLoop) { IsBackground = true, Name = "ConPTY reader " + ProcessId };
            reader.Start();
        }

        private void ReadLoop()
        {
            var buffer = new byte[16384];
            try
            {
                while (true)
                {
                    int read = _output.Read(buffer, 0, buffer.Length);
                    if (read <= 0) break;

                    var chunk = new byte[read];
                    Buffer.BlockCopy(buffer, 0, chunk, 0, read);
                    try { OutputReceived?.Invoke(chunk); } catch { }
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            finally
            {
                // The pipe breaks when the pseudo console closes, which happens after process exit.
                _processExited.WaitOne(5000);
                RaiseExited();
            }
        }

        private void OnProcessExited()
        {
            lock (_sync)
            {
                uint code;
                if (_processHandle != IntPtr.Zero && GetExitCodeProcess(_processHandle, out code))
                    _exitCode = unchecked((int)code);
            }
            _processExited.Set();

            // ClosePseudoConsole flushes the remaining output and then breaks the
            // output pipe, which lets ReadLoop finish. It may block until the output
            // is drained, so never call it on the reader thread.
            ThreadPool.QueueUserWorkItem(_ => ClosePseudoConsoleOnce());
        }

        private void RaiseExited()
        {
            if (Interlocked.Exchange(ref _exitRaised, 1) != 0) return;
            try { Exited?.Invoke(_exitCode); } catch { }
        }

        public void Write(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            lock (_sync)
            {
                if (_disposed || _input == null) return;
                try
                {
                    _input.Write(data, 0, data.Length);
                    _input.Flush();
                }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
            }
        }

        public void Write(string text)
        {
            if (!string.IsNullOrEmpty(text))
                Write(new UTF8Encoding(false).GetBytes(text));
        }

        public void Resize(short cols, short rows)
        {
            lock (_sync)
            {
                if (_pseudoConsole == IntPtr.Zero) return;
                ResizePseudoConsole(_pseudoConsole, new COORD { X = ClampSize(cols, 120), Y = ClampSize(rows, 30) });
            }
        }

        /// <summary>
        /// Closes the pseudo console (which sends CTRL_CLOSE_EVENT to every attached
        /// console process, including grandchildren) and terminates the root process.
        /// </summary>
        public void Kill()
        {
            ClosePseudoConsoleOnce();
            lock (_sync)
            {
                if (_processHandle != IntPtr.Zero && !HasExited)
                    TerminateProcess(_processHandle, 1);
            }
        }

        private void ClosePseudoConsoleOnce()
        {
            IntPtr handle;
            lock (_sync)
            {
                handle = _pseudoConsole;
                _pseudoConsole = IntPtr.Zero;
            }
            if (handle != IntPtr.Zero)
                ClosePseudoConsole(handle);
        }

        public void Dispose()
        {
            if (_disposed) return;
            Kill();
            _processExited.WaitOne(3000);

            lock (_sync)
            {
                _disposed = true;
                if (_exitWait != null) _exitWait.Unregister(null);
                _input?.Dispose();
                _output?.Dispose();
                if (_threadHandle != IntPtr.Zero) CloseHandle(_threadHandle);
                if (_processHandle != IntPtr.Zero) CloseHandle(_processHandle);
                _threadHandle = IntPtr.Zero;
                _processHandle = IntPtr.Zero;
            }
        }

        private static short ClampSize(short value, short fallback)
        {
            if (value <= 0) return fallback;
            return Math.Min(value, (short)1000);
        }

        private static string BuildEnvironmentBlock(IDictionary<string, string> overrides)
        {
            var variables = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
                variables[(string)entry.Key] = (string)entry.Value;

            if (overrides != null)
            {
                foreach (var pair in overrides)
                {
                    if (pair.Value == null) variables.Remove(pair.Key);
                    else variables[pair.Key] = pair.Value;
                }
            }

            var block = new StringBuilder();
            foreach (var pair in variables.Where(p => !string.IsNullOrEmpty(p.Key) && p.Key.IndexOf('=') < 0))
                block.Append(pair.Key).Append('=').Append(pair.Value).Append('\0');
            block.Append('\0');
            return block.ToString();
        }

        private sealed class ProcessWaitHandle : WaitHandle
        {
            public ProcessWaitHandle(IntPtr processHandle)
            {
                SafeWaitHandle = new SafeWaitHandle(processHandle, false);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COORD
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFOEX
        {
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, IntPtr lpPipeAttributes, int nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int CreatePseudoConsole(COORD size, SafeFileHandle hInput, SafeFileHandle hOutput, uint dwFlags, out IntPtr phPC);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int ResizePseudoConsole(IntPtr hPC, COORD size);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void ClosePseudoConsole(IntPtr hPC);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "CreateProcessW")]
        private static extern bool CreateProcess(string lpApplicationName, StringBuilder lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes,
            bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFOEX lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);
    }
}
