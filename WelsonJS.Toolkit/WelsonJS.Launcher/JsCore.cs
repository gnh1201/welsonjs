// JsCore.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Runtime.InteropServices;

namespace WelsonJS.Launcher
{
    public sealed class JsCore : IDisposable
    {
        private IntPtr _runtime = IntPtr.Zero;
        private IntPtr _context = IntPtr.Zero;
        private bool _disposed;

        public JsCore()
        {
            Check(JsCreateRuntime(0, IntPtr.Zero, out _runtime), nameof(JsCreateRuntime));
            Check(JsCreateContext(_runtime, out _context), nameof(JsCreateContext));
            Check(JsSetCurrentContext(_context), nameof(JsSetCurrentContext));
        }

        /// <summary>
        /// Evaluates JavaScript and returns the result converted to string (via JsConvertValueToString).
        /// </summary>
        public string EvaluateToString(string script, string sourceUrl = "repl")
        {
            if (_disposed) throw new ObjectDisposedException(nameof(JsCore));
            if (script is null) throw new ArgumentNullException(nameof(script));

            Check(JsRunScript(script, IntPtr.Zero, sourceUrl, out var result), nameof(JsRunScript));

            // Convert result -> JsString
            Check(JsConvertValueToString(result, out var jsString), nameof(JsConvertValueToString));

            // Extract pointer/length (UTF-16) and marshal to managed string
            Check(JsStringToPointer(jsString, out var p, out var len), nameof(JsStringToPointer));
            return Marshal.PtrToStringUni(p, checked((int)len));
        }

        /// <summary>
        /// Evaluates JavaScript for side effects; discards the result.
        /// </summary>
        public void Execute(string script, string sourceUrl = "repl")
        {
            _ = EvaluateToString(script, sourceUrl);
        }

        private static void Check(JsErrorCode code, string op)
        {
            if (code != JsErrorCode.JsNoError)
                throw new InvalidOperationException($"{op} failed with {code} (0x{(int)code:X}).");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                // Unset the current context from the SAME physical thread that set it.
                JsSetCurrentContext(IntPtr.Zero);
            }
            catch
            {
                // Swallow to ensure runtime is disposed.
            }
            finally
            {
                if (_runtime != IntPtr.Zero)
                {
                    JsDisposeRuntime(_runtime);
                    _runtime = IntPtr.Zero;
                }
                _context = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        ~JsCore() => Dispose();

        // =========================
        // P/Invoke surface (as given)
        // =========================

        // Essential (expanded) JsErrorCode set matching ChakraCore’s headers layout.
        // Values are grouped by category bases (0x10000, 0x20000, ...).
        public enum JsErrorCode
        {
            // Success
            JsNoError = 0,

            // Category bases (useful when inspecting ranges)
            JsErrorCategoryUsage = 0x10000,
            JsErrorCategoryEngine = 0x20000,
            JsErrorCategoryScript = 0x30000,
            JsErrorCategoryFatal = 0x40000,

            // Usage errors (0x10001+)
            JsErrorInvalidArgument = 0x10001,
            JsErrorNullArgument = 0x10002,
            JsErrorNoCurrentContext = 0x10003,
            JsErrorInExceptionState = 0x10004,
            JsErrorNotImplemented = 0x10005,
            JsErrorWrongThread = 0x10006,
            JsErrorRuntimeInUse = 0x10007,
            JsErrorBadSerializedScript = 0x10008,
            JsErrorInDisabledState = 0x10009,
            JsErrorCannotDisableExecution = 0x1000A,
            JsErrorHeapEnumInProgress = 0x1000B,
            JsErrorArgumentNotObject = 0x1000C,
            JsErrorInProfileCallback = 0x1000D,
            JsErrorInThreadServiceCallback = 0x1000E,
            JsErrorCannotSerializeDebugScript = 0x1000F,
            JsErrorAlreadyDebuggingContext = 0x10010,
            JsErrorAlreadyProfilingContext = 0x10011,
            JsErrorIdleNotEnabled = 0x10012,

            // Engine errors (0x20001+)
            JsErrorOutOfMemory = 0x20001,
            JsErrorBadFPUState = 0x20002,

            // Script errors (0x30001+)
            JsErrorScriptException = 0x30001,
            JsErrorScriptCompile = 0x30002,
            JsErrorScriptTerminated = 0x30003,
            JsErrorScriptEvalDisabled = 0x30004,

            // Fatal (0x40001)
            JsErrorFatal = 0x40001,

            // Misc/diagnostic (0x50000+)
            JsErrorWrongRuntime = 0x50000,
            JsErrorDiagAlreadyInDebugMode = 0x50001,
            JsErrorDiagNotInDebugMode = 0x50002,
            JsErrorDiagNotAtBreak = 0x50003,
            JsErrorDiagInvalidHandle = 0x50004,
            JsErrorDiagObjectNotFound = 0x50005,
            JsErrorDiagUnableToPerformAction = 0x50006,
        }

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern JsErrorCode JsCreateRuntime(uint attributes, IntPtr callback, out IntPtr runtime);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern JsErrorCode JsCreateContext(IntPtr runtime, out IntPtr context);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern JsErrorCode JsSetCurrentContext(IntPtr context);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        public static extern JsErrorCode JsRunScript(string script, IntPtr sourceContext, string sourceUrl, out IntPtr result);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern JsErrorCode JsConvertValueToString(IntPtr value, out IntPtr stringValue);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern JsErrorCode JsStringToPointer(IntPtr value, out IntPtr buffer, out UIntPtr length);

        // Note: Unsetting is typically done via JsSetCurrentContext(IntPtr.Zero)
        // Kept here only if your build exposes this symbol.
        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "JsSetCurrentContext")]
        public static extern JsErrorCode JsUnSetCurrentContext(IntPtr zero);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern JsErrorCode JsDisposeRuntime(IntPtr runtime);
    }
}
