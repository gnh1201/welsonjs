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
        public struct JsPropertyId { public IntPtr Handle; }

        public enum JsValueType
        {
            JsUndefined = 0,
            JsNull = 1,
            JsNumber = 2,
            JsString = 3,
            JsBoolean = 4,
            JsObject = 5,
            JsFunction = 6,
            JsError = 7,
            JsArray = 8,
            JsSymbol = 9,
            JsArrayBuffer = 10,
            JsTypedArray = 11,
            JsDataView = 12,
        }

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

        // === Values (used by JsonCodec to pass data without building script text) ===

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern JsErrorCode JsPointerToString(string value, UIntPtr length, out JsValue result);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsGetValueType(JsValue value, out JsValueType type);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsGetNullValue(out JsValue result);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsBoolToBoolean([MarshalAs(UnmanagedType.U1)] bool value, out JsValue result);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsBooleanToBool(JsValue value, [MarshalAs(UnmanagedType.U1)] out bool result);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsDoubleToNumber(double value, out JsValue result);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsNumberToDouble(JsValue value, out double result);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsIntToNumber(int value, out JsValue result);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsCreateObject(out JsValue result);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsCreateArray(uint length, out JsValue result);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsGetGlobalObject(out JsValue result);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        public static extern JsErrorCode JsGetPropertyIdFromName(string name, out JsPropertyId propertyId);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsGetProperty(JsValue obj, JsPropertyId propertyId, out JsValue value);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsSetProperty(JsValue obj, JsPropertyId propertyId, JsValue value, [MarshalAs(UnmanagedType.U1)] bool useStrictRules);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsGetIndexedProperty(JsValue obj, JsValue index, out JsValue value);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsSetIndexedProperty(JsValue obj, JsValue index, JsValue value);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsGetOwnPropertyNames(JsValue obj, out JsValue propertyNames);

        // arguments[0] is the "this" value
        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsCallFunction(JsValue function, JsValue[] arguments, ushort argumentCount, out JsValue result);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsGetAndClearException(out JsValue exception);

        // Roots a value for the engine GC while only managed code references it
        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsAddRef(JsValue value, out uint count);

        [DllImport("ChakraCore.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern JsErrorCode JsRelease(JsValue value, out uint count);
    }
}
