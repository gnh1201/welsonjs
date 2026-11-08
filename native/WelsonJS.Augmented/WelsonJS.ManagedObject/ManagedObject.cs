// ManagedObject.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Namhyeon Go, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WelsonJS.ManagedObject
{
    [ComVisible(true)]
    [Guid("f63b5f28-e278-497b-8525-2601ee7e8c3d")]
    [ProgId("WelsonJS.ManagedObject")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ManagedObject
    {
        // CLSID of the MSHTML document object ("htmlfile").
        private static readonly Guid HtmlDocumentClsid =
            new Guid("3050F55F-98B5-11CF-BB82-00AA00BDCE0B");

        // Managed (.NET) objects created by this instance. Wrap() only accepts
        // managed objects listed here, because late-bound reflection over an
        // arbitrary .NET object would expose members a script cannot otherwise
        // reach. While a script holds a managed object, its CCW keeps it alive,
        // so a weak table is sufficient and never extends object lifetimes.
        private readonly ConditionalWeakTable<object, object> _createdManagedObjects =
            new ConditionalWeakTable<object, object>();

        private readonly object _syncRoot = new object();

        // COM does not support method overloading (overloads are exported as
        // CreateObject, CreateObject_2, ...). app.js calls CreateObject with two
        // arguments, so a single method with an optional parameter is used.
        public object CreateObject(string progId, [Optional] object serverName)
        {
            if (string.IsNullOrWhiteSpace(progId))
                throw new ArgumentException("A ProgID is required.", nameof(progId));

            progId = progId.Trim();
            string server = NormalizeServerName(serverName);

            object obj;
            try
            {
                if (string.Equals(progId, "htmlfile", StringComparison.OrdinalIgnoreCase))
                {
                    obj = CreateFromClsid(HtmlDocumentClsid, server);
                }
                else
                {
                    if (string.Equals(progId, "welsonjs.toolkit", StringComparison.OrdinalIgnoreCase))
                        progId = "WelsonJS.Legacy.Toolkit";

                    Type type = Type.GetTypeFromProgID(progId, server, true);
                    obj = Activator.CreateInstance(type);
                }
            }
            catch (Exception ex)
            {
                Exception cause = (ex is TargetInvocationException && ex.InnerException != null)
                    ? ex.InnerException
                    : ex;

                // Preserve the original HRESULT so scripts can still inspect
                // e.number while getting a message that names the ProgID.
                throw new COMException(
                    BuildCreateErrorMessage(progId, server, cause),
                    cause.HResult
                );
            }

            Track(obj);
            RcwCollector.Observe(obj);

            return obj;
        }

        // Returns a late-binding wrapper that supports ByRef arguments (#429).
        //
        // Any COM object is accepted, regardless of how the script obtained it:
        // WScript.CreateObject (ADO, Scripting, WScript), GetObject, or the
        // return value of another call (WMI services, Office collections,
        // Shell folders). A script can already call these objects directly, so
        // the wrapper only adds ByRef support.
        //
        // Managed objects are accepted only if this instance created them.
        public object Wrap(object obj)
        {
            obj = NormalizeOptional(obj);
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            if (obj is ManagedObjectWrapper)
                return obj;

            if (!Marshal.IsComObject(obj) && !IsCreatedManagedObject(obj))
                throw new ArgumentException(
                    "Only COM objects, or managed objects created by ManagedObject, can be wrapped. " +
                    "Received: " + obj.GetType().FullName,
                    nameof(obj)
                );

            RcwCollector.Observe(obj);

            return new ManagedObjectWrapper(obj);
        }

        public object ByRef([Optional] object value)
        {
            return new ByRefArgument(NormalizeOptional(value));
        }

        // Releases COM objects that scripts no longer reference. COM objects
        // that pass through managed code are held by RCWs until the GC runs;
        // collection also runs automatically every CollectThreshold objects.
        // Call this after releasing large COM objects (for example, after
        // Excel.Application.Quit()) to release them immediately.
        public void Collect()
        {
            RcwCollector.Collect();
        }

        // Number of COM objects passing through ManagedObject after which a
        // collection runs automatically (process-wide). 0 disables it.
        public int CollectThreshold
        {
            get { return RcwCollector.Threshold; }
            set { RcwCollector.Threshold = value; }
        }

        public bool IsByRef([Optional] object value)
        {
            return value is ByRefArgument;
        }

        private void Track(object obj)
        {
            // COM objects need no tracking; see Wrap().
            if (obj == null || Marshal.IsComObject(obj))
                return;

            lock (_syncRoot)
            {
                object unused;
                if (!_createdManagedObjects.TryGetValue(obj, out unused))
                    _createdManagedObjects.Add(obj, null);
            }
        }

        private bool IsCreatedManagedObject(object obj)
        {
            lock (_syncRoot)
            {
                object unused;
                return _createdManagedObjects.TryGetValue(obj, out unused);
            }
        }

        private static object CreateFromClsid(Guid clsid, string server)
        {
            Type type = Type.GetTypeFromCLSID(clsid, server, true);
            return Activator.CreateInstance(type);
        }

        private static string NormalizeServerName(object serverName)
        {
            object value = NormalizeOptional(serverName);
            if (value == null)
                return null;

            string server = Convert.ToString(value).Trim();
            return server.Length == 0 ? null : server;
        }

        // Script engines pass omitted or undefined arguments as
        // Missing, DBNull, or VT_EMPTY (null).
        private static object NormalizeOptional(object value)
        {
            if (value == null || value is Missing || value is DBNull)
                return null;

            return value;
        }

        private static string BuildCreateErrorMessage(string progId, string server, Exception ex)
        {
            string target = server == null
                ? "\"" + progId + "\""
                : "\"" + progId + "\" on server \"" + server + "\"";

            return "Failed to create the COM object " + target + ": " + ex.Message;
        }
    }
}
