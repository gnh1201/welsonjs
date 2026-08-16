// ByRefArgument.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX - FileCopyrightText: 2026 Namhyeon Go, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
namespace WelsonJS.ManagedObject
{
    public sealed class ByRefArgument
    {
        public object Value { get; set; }

        public ByRefArgument()
        {
        }

        public ByRefArgument(object value)
        {
            Value = value;
        }
    }
}
