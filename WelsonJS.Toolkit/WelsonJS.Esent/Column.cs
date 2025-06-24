// Column.cs (WelsonJS.Esent)
// SPDX-License-Identifier: MIT
// SPDX-FileCopyrightText: 2025 Namhyeon Go, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Text;
using Microsoft.Isam.Esent.Interop;

namespace WelsonJS.Esent
{
    public class Column
    {
        public string Name { get; set; }
        public JET_coltyp Type { get; set; }
        public int MaxSize { get; set; }
        public JET_CP CodePage { get; set; }
        public bool IsPrimaryKey { get; set; } = false;

        public override string ToString()
        {
            return Name;
        }

        public static explicit operator string(Column c)
        {
            return c.ToString();
        }

        public Column(string name, JET_coltyp type, int maxSize = 0, JET_CP codePage = JET_CP.None)
        {
            Name = name;
            Type = type;
            MaxSize = maxSize;
            CodePage = codePage == JET_CP.None ?
                JET_CP.Unicode : codePage;
        }

        public Column(string name, Type dotNetType, int maxSize = 0, Encoding encoding = null)
        {
            Name = name;
            Type = GetJetColtypFromType(dotNetType);
            MaxSize = maxSize;
            CodePage = GetJetCpFromEncoding(encoding ?? Encoding.Unicode);
        }

        private static JET_coltyp GetJetColtypFromType(Type type)
        {
            if (type == typeof(string)) return JET_coltyp.Text;
            if (type == typeof(int)) return JET_coltyp.Long;
            if (type == typeof(long)) return JET_coltyp.Currency;
            if (type == typeof(bool)) return JET_coltyp.Bit;
            if (type == typeof(float)) return JET_coltyp.IEEESingle;
            if (type == typeof(double)) return JET_coltyp.IEEEDouble;
            if (type == typeof(DateTime)) return JET_coltyp.DateTime;
            if (type == typeof(byte[])) return JET_coltyp.LongBinary;

            throw new NotSupportedException($"Unsupported .NET type: {type.FullName}");
        }

        private static JET_CP GetJetCpFromEncoding(Encoding encoding)
        {
            if (encoding == Encoding.Unicode) return JET_CP.Unicode;
            if (encoding == Encoding.ASCII) return JET_CP.ASCII;
            if (encoding.CodePage == 1252) return (JET_CP)1252; // Windows-1252 / Latin1
            if (encoding.CodePage == 51949) return (JET_CP)51949; // EUC-KR
            if (encoding.CodePage == 949) return (JET_CP)949; // UHC (Windows Korean)
            if (encoding.CodePage == 932) return (JET_CP)932; // Shift-JIS (Japanese)
            if (encoding.CodePage == 936) return (JET_CP)936; // GB2312 (Simplified Chinese)
            if (encoding.CodePage == 65001) return (JET_CP)65001; // UTF-8
            if (encoding.CodePage == 28591) return (JET_CP)28591; // ISO-8859-1

            throw new NotSupportedException($"Unsupported encoding: {encoding.WebName} (code page {encoding.CodePage})");
        }
    }
}
