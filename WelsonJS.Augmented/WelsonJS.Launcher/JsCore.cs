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
    public sealed class JsCore : IDisposable
    {
        private JsNative.JsRuntime _rt;
        private JsNative.JsContext _ctx;
        private bool _disposed;

        public JsCore()
        {
            Check(JsNative.JsCreateRuntime(JsNative.JsRuntimeAttributes.None, null, out _rt), "JsCreateRuntime");
            Check(JsNative.JsCreateContext(_rt, out _ctx), "JsCreateContext");
            Check(JsNative.JsSetCurrentContext(_ctx), "JsSetCurrentContext");
        }

        public string EvaluateToString(string script, string sourceUrl = "repl")
        {
            if (_disposed) throw new ObjectDisposedException(nameof(JsCore));
            if (script == null) throw new ArgumentNullException(nameof(script));

            JsNative.JsValue result;
            Check(JsNative.JsRunScript(script, UIntPtr.Zero, sourceUrl, out result), "JsRunScript");

            JsNative.JsValue jsStr;
            Check(JsNative.JsConvertValueToString(result, out jsStr), "JsConvertValueToString");

            IntPtr p;
            UIntPtr len;
            Check(JsNative.JsStringToPointer(jsStr, out p, out len), "JsStringToPointer");

            int chars = checked((int)len);
            return Marshal.PtrToStringUni(p, chars);
        }

        private static void Check(JsNative.JsErrorCode code, string op)
        {
            if (code != JsNative.JsErrorCode.JsNoError)
                throw new InvalidOperationException(op + " failed: " + code + " (0x" + ((int)code).ToString("X", CultureInfo.InvariantCulture) + ")");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                // Unset current context
                JsNative.JsSetCurrentContext(new JsNative.JsContext { Handle = IntPtr.Zero });
            }
            catch { /* ignore */ }
            finally
            {
                if (_rt.Handle != IntPtr.Zero)
                    JsNative.JsDisposeRuntime(_rt);
            }
            GC.SuppressFinalize(this);
        }

        ~JsCore() { Dispose(); }
    }
}
