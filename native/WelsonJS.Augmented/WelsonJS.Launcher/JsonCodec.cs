// JsonCodec.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WelsonJS.Launcher
{
    /// <summary>
    /// The launcher's single JSON implementation. Parsing and formatting are done by
    /// ChakraCore's JSON.parse / JSON.stringify; values cross the boundary through the
    /// JsRT value API, never as script text.
    ///
    /// .NET → JSON: null, string, char, bool, numbers, enums (as numbers), Guid,
    /// DateTime/DateTimeOffset (ISO 8601 UTC), byte[] (base64), IDictionary (object),
    /// IEnumerable (array), and any other object by its public properties and fields.
    ///
    /// JSON → .NET: Dictionary&lt;string, object&gt;, object[], string, bool, null, and
    /// numbers as int, long (integral values up to 2^53 - 1) or double.
    /// </summary>
    public static class JsonCodec
    {
        private const int MaxDepth = 64;
        private const double MaxSafeInteger = 9007199254740991d; // Number.MAX_SAFE_INTEGER

        private static readonly object InitSync = new object();
        private static JsCore _core;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr GetModuleHandle(string moduleName);

        /// <summary>
        /// True once ChakraCore.dll is loaded (AssemblyLoader.LoadNativeModules does this at
        /// startup after verifying it). Using the codec earlier would make P/Invoke load
        /// whatever ChakraCore.dll the DLL search path finds first.
        /// </summary>
        public static bool IsAvailable
        {
            get { return GetModuleHandle("ChakraCore.dll") != IntPtr.Zero; }
        }

        private static JsCore Core
        {
            get
            {
                lock (InitSync)
                {
                    if (_core == null)
                    {
                        if (!IsAvailable)
                            throw new InvalidOperationException("ChakraCore.dll is not loaded yet.");
                        _core = new JsCore();
                    }
                    return _core;
                }
            }
        }

        public static string Serialize(object value, int space = 0)
        {
            space = Clamp(space, 0, 10);
            return Core.Run(() =>
            {
                JsNative.JsValue js = ToJs(value, new HashSet<object>(ReferenceComparer.Instance), 0);
                return Stringify(js, space);
            });
        }

        /// <summary>Parses any JSON value. Throws <see cref="FormatException"/> on invalid JSON.</summary>
        public static object Deserialize(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            return Core.Run(() =>
            {
                JsNative.JsValue root = Parse(json);
                Pin(root);
                try { return FromJs(root, 0); }
                finally { Unpin(root); }
            });
        }

        /// <summary>Parses a JSON object. Throws <see cref="FormatException"/> if the text is not one.</summary>
        public static Dictionary<string, object> DeserializeObject(string json)
        {
            var result = Deserialize(json) as Dictionary<string, object>;
            if (result == null)
                throw new FormatException("JSON value is not an object.");
            return result;
        }

        /// <summary>Re-formats JSON text: 0 minifies, 1-10 indents by that many spaces.</summary>
        public static string Format(string json, int space)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            space = Clamp(space, 0, 10);
            return Core.Run(() => Stringify(Parse(json), space));
        }

        public static bool IsValid(string json)
        {
            if (json == null) return false;
            try
            {
                Core.Run(() => Parse(json));
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        // ---------------- JSON.parse / JSON.stringify ----------------

        private static JsNative.JsValue Parse(string json)
        {
            try
            {
                return CallJson("parse", ToJsString(json));
            }
            catch (JsScriptException ex)
            {
                throw new FormatException("Invalid JSON: " + ex.Message, ex);
            }
        }

        private static string Stringify(JsNative.JsValue value, int space)
        {
            JsNative.JsValue result = CallJson("stringify", value, Null(), Number(space));

            JsNative.JsValueType type;
            JsCore.Check(JsNative.JsGetValueType(result, out type), "JsGetValueType");
            return type == JsNative.JsValueType.JsString ? JsCore.ToClrString(result) : "null";
        }

        private static JsNative.JsValue CallJson(string name, params JsNative.JsValue[] args)
        {
            // The arguments live only in managed memory until the call, and looking up
            // JSON.<name> can allocate (and so collect), so root them first.
            int pinned = 0;
            try
            {
                for (; pinned < args.Length; pinned++)
                    Pin(args[pinned]);

                JsNative.JsValue global, json, function, result;
                JsCore.Check(JsNative.JsGetGlobalObject(out global), "JsGetGlobalObject");
                JsCore.Check(JsNative.JsGetProperty(global, PropertyId("JSON"), out json), "JsGetProperty(JSON)");
                JsCore.Check(JsNative.JsGetProperty(json, PropertyId(name), out function), "JsGetProperty(JSON." + name + ")");

                var argv = new JsNative.JsValue[args.Length + 1];
                argv[0] = json;
                Array.Copy(args, 0, argv, 1, args.Length);
                JsCore.Check(JsNative.JsCallFunction(function, argv, (ushort)argv.Length, out result), "JSON." + name);
                return result;
            }
            finally
            {
                while (pinned > 0)
                    Unpin(args[--pinned]);
            }
        }

        // ---------------- .NET → JS ----------------

        private static JsNative.JsValue ToJs(object value, HashSet<object> seen, int depth)
        {
            if (value == null || value is DBNull || depth > MaxDepth)
                return Null();

            if (value is string) return ToJsString((string)value);
            if (value is char) return ToJsString(value.ToString());
            if (value is bool)
            {
                JsNative.JsValue b;
                JsCore.Check(JsNative.JsBoolToBoolean((bool)value, out b), "JsBoolToBoolean");
                return b;
            }
            if (value is Enum || IsNumeric(value))
                return Number(Convert.ToDouble(value, CultureInfo.InvariantCulture));
            if (value is Guid) return ToJsString(value.ToString());
            if (value is DateTime)
                return ToJsString(((DateTime)value).ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            if (value is DateTimeOffset)
                return ToJsString(((DateTimeOffset)value).ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            if (value is byte[]) return ToJsString(Convert.ToBase64String((byte[])value));

            // Containers: a value already on the current path is a cycle
            if (!seen.Add(value))
                return Null();

            // A container is unreachable for the engine GC until its parent holds it,
            // and filling it allocates, so keep it rooted while it is being filled.
            JsNative.JsValue container;
            if (value is IEnumerable && !(value is IDictionary))
                JsCore.Check(JsNative.JsCreateArray(0, out container), "JsCreateArray");
            else
                container = NewObject();
            Pin(container);
            try
            {
                if (value is IDictionary)
                {
                    foreach (DictionaryEntry entry in (IDictionary)value)
                    {
                        string key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
                        if (key != null)
                            SetProperty(container, key, ToJs(entry.Value, seen, depth + 1));
                    }
                }
                else if (value is IEnumerable)
                {
                    int i = 0;
                    foreach (object item in (IEnumerable)value)
                    {
                        JsNative.JsValue index;
                        JsCore.Check(JsNative.JsIntToNumber(i++, out index), "JsIntToNumber");
                        JsCore.Check(JsNative.JsSetIndexedProperty(container, index, ToJs(item, seen, depth + 1)), "JsSetIndexedProperty");
                    }
                }
                else
                {
                    // Plain objects, including anonymous types
                    Type type = value.GetType();
                    foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (property.CanRead && property.GetIndexParameters().Length == 0)
                            SetProperty(container, property.Name, ToJs(property.GetValue(value, null), seen, depth + 1));
                    }
                    foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                        SetProperty(container, field.Name, ToJs(field.GetValue(value), seen, depth + 1));
                }
                return container;
            }
            finally
            {
                Unpin(container);
                seen.Remove(value);
            }
        }

        // ---------------- JS → .NET ----------------

        private static object FromJs(JsNative.JsValue value, int depth)
        {
            // Untrusted input reaches here; bound the recursion instead of overflowing the stack.
            if (depth > MaxDepth)
                throw new FormatException("JSON nesting is deeper than " + MaxDepth + " levels.");

            JsNative.JsValueType type;
            JsCore.Check(JsNative.JsGetValueType(value, out type), "JsGetValueType");

            switch (type)
            {
                case JsNative.JsValueType.JsUndefined:
                case JsNative.JsValueType.JsNull:
                    return null;

                case JsNative.JsValueType.JsString:
                    return JsCore.ToClrString(value);

                case JsNative.JsValueType.JsBoolean:
                    bool b;
                    JsCore.Check(JsNative.JsBooleanToBool(value, out b), "JsBooleanToBool");
                    return b;

                case JsNative.JsValueType.JsNumber:
                    double d;
                    JsCore.Check(JsNative.JsNumberToDouble(value, out d), "JsNumberToDouble");
                    if (Math.Floor(d) == d && Math.Abs(d) <= MaxSafeInteger)
                    {
                        if (d >= int.MinValue && d <= int.MaxValue) return (int)d;
                        return (long)d;
                    }
                    return d;

                case JsNative.JsValueType.JsArray:
                    int length = ToInt(GetProperty(value, "length"));
                    var items = new object[length];
                    for (int i = 0; i < length; i++)
                        items[i] = FromJs(GetIndex(value, i), depth + 1);
                    return items;

                default:
                    JsNative.JsValue names;
                    JsCore.Check(JsNative.JsGetOwnPropertyNames(value, out names), "JsGetOwnPropertyNames");
                    Pin(names); // a fresh array that nothing else references
                    try
                    {
                        int count = ToInt(GetProperty(names, "length"));
                        var map = new Dictionary<string, object>(count, StringComparer.Ordinal);
                        for (int i = 0; i < count; i++)
                        {
                            string name = JsCore.ToClrString(GetIndex(names, i));
                            map[name] = FromJs(GetProperty(value, name), depth + 1);
                        }
                        return map;
                    }
                    finally
                    {
                        Unpin(names);
                    }
            }
        }

        // ---------------- Helpers ----------------

        private static JsNative.JsValue ToJsString(string s)
        {
            JsNative.JsValue v;
            JsCore.Check(JsNative.JsPointerToString(s, (UIntPtr)s.Length, out v), "JsPointerToString");
            return v;
        }

        private static JsNative.JsValue Number(double d)
        {
            JsNative.JsValue v;
            JsCore.Check(JsNative.JsDoubleToNumber(d, out v), "JsDoubleToNumber");
            return v;
        }

        private static JsNative.JsValue Null()
        {
            JsNative.JsValue v;
            JsCore.Check(JsNative.JsGetNullValue(out v), "JsGetNullValue");
            return v;
        }

        private static JsNative.JsValue NewObject()
        {
            JsNative.JsValue v;
            JsCore.Check(JsNative.JsCreateObject(out v), "JsCreateObject");
            return v;
        }

        private static JsNative.JsPropertyId PropertyId(string name)
        {
            JsNative.JsPropertyId id;
            JsCore.Check(JsNative.JsGetPropertyIdFromName(name, out id), "JsGetPropertyIdFromName");
            return id;
        }

        private static JsNative.JsValue GetProperty(JsNative.JsValue obj, string name)
        {
            JsNative.JsValue v;
            JsCore.Check(JsNative.JsGetProperty(obj, PropertyId(name), out v), "JsGetProperty");
            return v;
        }

        private static void SetProperty(JsNative.JsValue obj, string name, JsNative.JsValue value)
        {
            // Looking up the property id can allocate before value is attached
            Pin(value);
            try
            {
                JsCore.Check(JsNative.JsSetProperty(obj, PropertyId(name), value, true), "JsSetProperty");
            }
            finally
            {
                Unpin(value);
            }
        }

        // ChakraCore scans only native memory for roots, so values held by managed code
        // across calls that may allocate must be rooted explicitly.
        private static void Pin(JsNative.JsValue value)
        {
            uint count;
            JsCore.Check(JsNative.JsAddRef(value, out count), "JsAddRef");
        }

        private static void Unpin(JsNative.JsValue value)
        {
            uint count;
            JsNative.JsRelease(value, out count);
        }

        private static JsNative.JsValue GetIndex(JsNative.JsValue obj, int index)
        {
            JsNative.JsValue i, v;
            JsCore.Check(JsNative.JsIntToNumber(index, out i), "JsIntToNumber");
            JsCore.Check(JsNative.JsGetIndexedProperty(obj, i, out v), "JsGetIndexedProperty");
            return v;
        }

        private static int ToInt(JsNative.JsValue value)
        {
            double d;
            JsCore.Check(JsNative.JsNumberToDouble(value, out d), "JsNumberToDouble");
            return checked((int)d);
        }

        private static bool IsNumeric(object v)
        {
            return v is byte || v is sbyte ||
                   v is short || v is ushort ||
                   v is int || v is uint ||
                   v is long || v is ulong ||
                   v is float || v is double || v is decimal;
        }

        private static int Clamp(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        /// <summary>Reference equality for cycle detection (works on .NET Framework).</summary>
        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();
            bool IEqualityComparer<object>.Equals(object x, object y) { return ReferenceEquals(x, y); }
            int IEqualityComparer<object>.GetHashCode(object obj) { return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj); }
        }
    }
}
