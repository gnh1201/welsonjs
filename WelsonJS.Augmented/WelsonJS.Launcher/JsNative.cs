// JsNative.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Runtime.InteropServices;

namespace WelsonJS.Launcher
{
    public static class JsNative
    {
        // === Enums / handles ===
        [Flags]
        public enum JsRuntimeAttributes : uint
        {
            None = 0x00000000,
            DisableBackgroundWork = 0x00000001,
            AllowScriptInterrupt = 0x00000002,
            EnableIdleProcessing = 0x00000004,
            DisableNativeCodeGeneration = 0x00000008,
            EnableExperimentalFeatures = 0x00000010,
        }

        // ChakraCore typedefs are opaque pointers; represent as IntPtr
        public struct JsRuntime { public IntPtr Handle; }
        public struct JsContext { public IntPtr Handle; }
        public struct JsValue { public IntPtr Handle; }

        // JsErrorCode (essential subset; expand as needed)
        public enum JsErrorCode
        {
            JsNoError = 0,

            // Usage
            JsErrorInvalidArgument = 0x10001,
            JsErrorNullArgument = 0x10002,
            JsErrorNoCurrentContext = 0x10003,
            JsErrorInExceptionState = 0x10004,
            JsErrorNotImplemented = 0x10005,
            JsErrorWrongThread = 0x10006,
            JsErrorRuntimeInUse = 0x10007,

            // Script
            JsErrorScriptException = 0x30001,
            JsErrorScriptCompile = 0x30002,
            JsErrorScriptTerminated = 0x30003,

            // Engine
            JsErrorOutOfMemory = 0x20001,

            // Fatal
            JsErrorFatal = 0x40001,
        }

        // Thread service callback: __stdcall
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate JsErrorCode JsThreadServiceCallback(IntPtr callback, IntPtr callbackState);

        // ======= FIXED SIGNATURES (StdCall + Unicode) =======

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsCreateRuntime(
            JsRuntimeAttributes attributes,
            JsThreadServiceCallback threadService,               // pass null if unused
            out JsRuntime runtime);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsCreateContext(
            JsRuntime runtime,
            out JsContext newContext);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsSetCurrentContext(JsContext context);

        // JsSourceContext is size_t → UIntPtr; strings are wide-char
        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern JsErrorCode JsRunScript(
            string script,
            UIntPtr sourceContext,
            string sourceUrl,
            out JsValue result);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsConvertValueToString(JsValue value, out JsValue stringValue);

        // Returns pointer to UTF-16 buffer + length (size_t) for a JsString value
        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsStringToPointer(
            JsValue value,
            out IntPtr buffer,
            out UIntPtr length);

        // Unset by passing "invalid" context (JS_INVALID_REFERENCE is typically null)
        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsDisposeRuntime(JsRuntime runtime);
    }
}
