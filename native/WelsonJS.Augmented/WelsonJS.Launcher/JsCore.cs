// JsCore.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace WelsonJS.Launcher
{
    public sealed class JsScriptException : InvalidOperationException
    {
        public JsScriptException(string message) : base(message) { }
    }

    /// <summary>
    /// One ChakraCore runtime/context. ChakraCore runtimes may move between threads but
    /// may be active on only one at a time, so every use goes through <see cref="Run{T}"/>,
    /// which serializes callers and makes the context current only for the call.
    /// </summary>
    public sealed class JsCore : IDisposable
    {
        private readonly object _sync = new object();
        private JsNative.JsRuntime _rt;
        private JsNative.JsContext _ctx;
        private int _depth;
        private bool _disposed;

        public JsCore()
        {
            Check(JsNative.JsCreateRuntime(JsNative.JsRuntimeAttributes.None, null, out _rt), "JsCreateRuntime");
            Check(JsNative.JsCreateContext(_rt, out _ctx), "JsCreateContext");
        }

        public T Run<T>(Func<T> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            lock (_sync)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(JsCore));

                // Re-entrant calls on the same thread (e.g. a trace listener invoked from
                // inside a call) keep the context that the outer call made current.
                if (_depth++ == 0)
                    Check(JsNative.JsSetCurrentContext(_ctx), "JsSetCurrentContext");
                try
                {
                    return action();
                }
                finally
                {
                    if (--_depth == 0)
                        JsNative.JsSetCurrentContext(new JsNative.JsContext { Handle = IntPtr.Zero });
                }
            }
        }

        public string EvaluateToString(string script, string sourceUrl = "repl")
        {
            if (script == null) throw new ArgumentNullException(nameof(script));

            return Run(() =>
            {
                JsNative.JsValue result;
                Check(JsNative.JsRunScript(script, UIntPtr.Zero, sourceUrl, out result), "JsRunScript");

                JsNative.JsValue jsStr;
                Check(JsNative.JsConvertValueToString(result, out jsStr), "JsConvertValueToString");
                return ToClrString(jsStr);
            });
        }

        /// <summary>Reads a JS string value. Call inside <see cref="Run{T}"/>.</summary>
        public static string ToClrString(JsNative.JsValue value)
        {
            IntPtr p;
            UIntPtr len;
            Check(JsNative.JsStringToPointer(value, out p, out len), "JsStringToPointer");
            return Marshal.PtrToStringUni(p, checked((int)len));
        }

        /// <summary>
        /// Throws on failure. A script exception is cleared from the runtime and its
        /// message is reported, so the runtime stays usable for the next call.
        /// </summary>
        public static void Check(JsNative.JsErrorCode code, string op)
        {
            if (code == JsNative.JsErrorCode.JsNoError)
                return;

            if (code == JsNative.JsErrorCode.JsErrorScriptException || code == JsNative.JsErrorCode.JsErrorInExceptionState)
                throw new JsScriptException(op + " failed: " + TakeExceptionMessage());

            throw new InvalidOperationException(op + " failed: " + code + " (0x" + ((int)code).ToString("X", CultureInfo.InvariantCulture) + ")");
        }

        private static string TakeExceptionMessage()
        {
            JsNative.JsValue exception, text;
            if (JsNative.JsGetAndClearException(out exception) != JsNative.JsErrorCode.JsNoError)
                return "unknown script error";
            if (JsNative.JsConvertValueToString(exception, out text) != JsNative.JsErrorCode.JsNoError)
                return "unknown script error";

            IntPtr p;
            UIntPtr len;
            if (JsNative.JsStringToPointer(text, out p, out len) != JsNative.JsErrorCode.JsNoError)
                return "unknown script error";
            return Marshal.PtrToStringUni(p, checked((int)len));
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed) return;
                _disposed = true;

                if (_rt.Handle != IntPtr.Zero)
                    JsNative.JsDisposeRuntime(_rt);
            }
            GC.SuppressFinalize(this);
        }

        ~JsCore() { Dispose(); }
    }
}
