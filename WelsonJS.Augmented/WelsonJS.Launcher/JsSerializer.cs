// JsSerializer.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WelsonJS.Launcher
{
    public sealed class JsSerializer : IDisposable
    {
        private readonly JsCore _core;
        private readonly bool _ownsCore;

        // In-engine parsed document store management
        private bool _storeReady;
        private int _nextId = 1; // 0 is reserved (unused)

        public JsSerializer() : this(new JsCore(), true) { }

        public JsSerializer(JsCore core, bool ownsCore)
        {
            if (core == null) throw new ArgumentNullException("core");
            _core = core;
            _ownsCore = ownsCore;
        }

        // ---------------- Engine-backed document store ----------------

        /// <summary>
        /// Parses JSON once and stores it in the engine under a numeric id.
        /// Returns the id which can be used for fast repeated extraction.
        /// </summary>
        public int Load(string json)
        {
            if (json == null) throw new ArgumentNullException("json");
            EnsureStore();

            int id = _nextId++;
            // Create slot and parse
            // Using Object.create(null) for a clean dictionary without prototype.
            var sb = new StringBuilder();
            sb.Append("(function(){var S=globalThis.__WJ_STORE;");
            sb.Append("S[").Append(id.ToString(CultureInfo.InvariantCulture)).Append("]=JSON.parse(").Append(Q(json)).Append(");");
            sb.Append("return '1';})()");
            string r = _core.EvaluateToString(sb.ToString());
            if (r != "1") throw new InvalidOperationException("Failed to load JSON into the engine store.");
            return id;
        }

        /// <summary>
        /// Removes a previously loaded document from the engine store.
        /// After this, the id becomes invalid.
        /// </summary>
        public void Unload(int id)
        {
            EnsureStore();
            string script = "(function(){var S=globalThis.__WJ_STORE; delete S[" + id.ToString(CultureInfo.InvariantCulture) + "]; return '1';})()";
            _core.EvaluateToString(script);
        }

        /// <summary>
        /// Replaces the stored JSON at a given id (parse once, reuse later).
        /// </summary>
        public void Reload(int id, string json)
        {
            if (json == null) throw new ArgumentNullException("json");
            EnsureStore();
            string script = "(function(){var S=globalThis.__WJ_STORE; S[" + id.ToString(CultureInfo.InvariantCulture) + "]=JSON.parse(" + Q(json) + "); return '1';})()";
            _core.EvaluateToString(script);
        }

        /// <summary>
        /// Stringifies the stored value identified by id (no reparse).
        /// </summary>
        public string ToJson(int id, int space)
        {
            EnsureStore();
            space = Clamp(space, 0, 10);
            string script = "(function(){var v=globalThis.__WJ_STORE[" + id.ToString(CultureInfo.InvariantCulture) + "]; return JSON.stringify(v,null," + space.ToString(CultureInfo.InvariantCulture) + ");})()";
            return _core.EvaluateToString(script);
        }

        /// <summary>
        /// Extracts from a stored document identified by id (no reparse).
        /// Returns JSON of the selected value.
        /// </summary>
        public string ExtractFrom(int id, params object[] path)
        {
            EnsureStore();
            if (path == null) path = new object[0];
            string jsPath = BuildJsPath(path);

            var sb = new StringBuilder();
            sb.Append("(function(){var v=globalThis.__WJ_STORE[")
              .Append(id.ToString(CultureInfo.InvariantCulture))
              .Append("];var p=").Append(jsPath).Append(";");
            sb.Append("for(var i=0;i<p.length;i++){var k=p[i];");
            sb.Append("if(Array.isArray(v) && typeof k==='number'){ v=v[k]; }");
            sb.Append("else { v=(v==null?null:v[k]); }}");
            sb.Append("return JSON.stringify(v);})()");
            return _core.EvaluateToString(sb.ToString());
        }

        // Initialize the global store only once per JsSerializer instance/context
        private void EnsureStore()
        {
            if (_storeReady) return;
            // Create a single global dictionary: globalThis.__WJ_STORE
            // Object.create(null) prevents prototype pollution and accidental hits on built-ins.
            string script =
                "(function(){var g=globalThis||this;" +
                "if(!g.__WJ_STORE){Object.defineProperty(g,'__WJ_STORE',{value:Object.create(null),writable:false,enumerable:false,configurable:false});}" +
                "return '1';})()";
            string r = _core.EvaluateToString(script);
            _storeReady = (r == "1");
            if (!_storeReady) throw new InvalidOperationException("Failed to initialize the engine-backed JSON store.");
        }

        // ---------------- Existing API (kept for compatibility) ----------------

        public bool IsValid(string json)
        {
            if (json == null) throw new ArgumentNullException("json");
            string script =
                "(function(){try{JSON.parse(" + Q(json) + ");return '1';}catch(_){return '0';}})()";
            string r = _core.EvaluateToString(script);
            return r == "1";
        }

        public string Minify(string json)
        {
            if (json == null) throw new ArgumentNullException("json");
            string script = "JSON.stringify(JSON.parse(" + Q(json) + "))";
            return _core.EvaluateToString(script);
        }

        public string Pretty(string json, int space)
        {
            if (json == null) throw new ArgumentNullException("json");
            space = Clamp(space, 0, 10);
            string script = "JSON.stringify(JSON.parse(" + Q(json) + "),null," + space.ToString(CultureInfo.InvariantCulture) + ")";
            return _core.EvaluateToString(script);
        }

        public string Normalize(string json)
        {
            return Minify(json);
        }

        /// <summary>
        /// Extracts by string-only path (kept for backward compatibility).
        /// Internally forwards to the mixed-path overload.
        /// </summary>
        public string Extract(string json, params string[] path)
        {
            if (path == null) path = new string[0];
            object[] mixed = new object[path.Length];
            for (int i = 0; i < path.Length; i++) mixed[i] = path[i];
            return Extract(json, mixed);
        }

        /// <summary>
        /// Extracts by a mixed path directly from a JSON string (parses every call).
        /// Prefer Load(...) + ExtractFrom(...) to avoid repeated parsing.
        /// </summary>
        public string Extract(string json, params object[] path)
        {
            if (json == null) throw new ArgumentNullException("json");
            if (path == null) path = new object[0];

            string jsPath = BuildJsPath(path);

            var sb = new StringBuilder();
            sb.Append("(function(){var v=JSON.parse(").Append(Q(json)).Append(");");
            sb.Append("var p=").Append(jsPath).Append(";");
            sb.Append("for(var i=0;i<p.length;i++){var k=p[i];");
            sb.Append("if(Array.isArray(v) && typeof k==='number'){ v=v[k]; }");
            sb.Append("else { v=(v==null?null:v[k]); }}");
            sb.Append("return JSON.stringify(v);})()");
            return _core.EvaluateToString(sb.ToString());
        }

        public string Serialize(object value, int space)
        {
            space = Clamp(space, 0, 10);
            string expr = BuildJsExpression(value, new HashSet<object>(ReferenceEqualityComparer.Instance), 0);
            string script = "JSON.stringify((" + expr + "),null," + space.ToString(CultureInfo.InvariantCulture) + ")";
            return _core.EvaluateToString(script);
        }

        // ---------------- Helpers ----------------

        private static int Clamp(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        /// <summary>
        /// Encode a .NET string as a JS double-quoted string literal.
        /// </summary>
        private static string Q(string s)
        {
            if (s == null) return "null";
            var sb = new StringBuilder(s.Length + 16);
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                switch (ch)
                {
                    case '\\': sb.Append(@"\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\b': sb.Append(@"\b"); break;
                    case '\f': sb.Append(@"\f"); break;
                    case '\n': sb.Append(@"\n"); break;
                    case '\r': sb.Append(@"\r"); break;
                    case '\t': sb.Append(@"\t"); break;
                    case '\u2028': sb.Append("\\u2028"); break;
                    case '\u2029': sb.Append("\\u2029"); break;
                    default:
                        if (char.IsControl(ch))
                        {
                            sb.Append("\\u");
                            sb.Append(((int)ch).ToString("X4"));
                        }
                        else
                        {
                            sb.Append(ch);
                        }
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        /// <summary>
        /// Builds a JS array literal representing the path.
        /// Numeric segments are emitted as numbers; others as strings.
        /// </summary>
        private static string BuildJsPath(object[] segments)
        {
            if (segments == null || segments.Length == 0) return "[]";

            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < segments.Length; i++)
            {
                if (i > 0) sb.Append(',');

                object seg = segments[i];

                // Treat integral types as numbers for array indexing
                if (seg is sbyte || seg is byte ||
                    seg is short || seg is ushort ||
                    seg is int || seg is uint ||
                    seg is long || seg is ulong)
                {
                    sb.Append(Convert.ToString(seg, CultureInfo.InvariantCulture));
                }
                else
                {
                    string str = (seg == null) ? string.Empty : Convert.ToString(seg, CultureInfo.InvariantCulture);
                    sb.Append(Q(str));
                }
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static bool IsNumeric(object v)
        {
            if (v == null) return false;
            Type t = v.GetType();
            t = Nullable.GetUnderlyingType(t) ?? t;
            return t == typeof(byte) || t == typeof(sbyte) ||
                   t == typeof(short) || t == typeof(ushort) ||
                   t == typeof(int) || t == typeof(uint) ||
                   t == typeof(long) || t == typeof(ulong) ||
                   t == typeof(float) || t == typeof(double) ||
                   t == typeof(decimal);
        }

        private static bool IsImmutableLike(object v)
        {
            return v is string || v is bool ||
                   v is byte || v is sbyte ||
                   v is short || v is ushort ||
                   v is int || v is uint ||
                   v is long || v is ulong ||
                   v is float || v is double || v is decimal ||
                   v is Guid || v is DateTime || v is DateTimeOffset;
        }

        /// <summary>
        /// Builds a safe JS expression for a .NET value (no engine calls here).
        /// Engine will stringify the produced expression via JSON.stringify.
        /// </summary>
        private static string BuildJsExpression(object value, HashSet<object> seen, int depth)
        {
            if (depth > 64) return "null"; // depth guard

            if (value == null) return "null";

            // Primitives
            if (value is string) return Q((string)value);
            if (value is bool) return ((bool)value) ? "true" : "false";
            if (IsNumeric(value))
                return Convert.ToString(value, CultureInfo.InvariantCulture);

            // Common value-like types → stringify as JS strings
            if (value is Guid)
                return Q(((Guid)value).ToString());
            if (value is DateTime)
                return Q(((DateTime)value).ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            if (value is DateTimeOffset)
                return Q(((DateTimeOffset)value).ToUniversalTime().ToString("o", CultureInfo.InvariantCulture));
            if (value is byte[])
                return Q(Convert.ToBase64String((byte[])value));

            // Prevent circular refs for reference types
            if (!IsImmutableLike(value) && !seen.Add(value))
                return "null";

            // IDictionary (string keys only)
            if (value is IDictionary)
            {
                var map = (IDictionary)value;
                var sb = new StringBuilder();
                sb.Append('{');
                bool first = true;
                foreach (DictionaryEntry kv in map)
                {
                    string key = kv.Key as string;
                    if (key == null) continue; // JSON keys must be strings
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append(Q(key)).Append(':')
                      .Append(BuildJsExpression(kv.Value, seen, depth + 1));
                }
                sb.Append('}');
                return sb.ToString();
            }

            // IEnumerable → array
            if (value is IEnumerable)
            {
                var seq = (IEnumerable)value;
                var sb = new StringBuilder();
                sb.Append('[');
                bool first = true;
                foreach (object item in seq)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append(BuildJsExpression(item, seen, depth + 1));
                }
                sb.Append(']');
                return sb.ToString();
            }

            // Fallback → ToString() as JS string
            string s = value.ToString();
            return Q(s ?? string.Empty);
        }

        public void Dispose()
        {
            if (_ownsCore)
                _core.Dispose();
        }

        /// <summary>
        /// Reference equality comparer for cycle detection (works on .NET Framework).
        /// </summary>
        private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            private ReferenceEqualityComparer() { }
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
            bool IEqualityComparer<object>.Equals(object x, object y) { return object.ReferenceEquals(x, y); }
            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
