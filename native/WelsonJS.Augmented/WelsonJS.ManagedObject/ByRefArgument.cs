// ByRefArgument.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Namhyeon Go, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System.Runtime.InteropServices;

namespace WelsonJS.ManagedObject
{
    // Returned to scripts by ManagedObject.ByRef(). It must be COM visible,
    // otherwise scripts cannot read the Value property through IDispatch.
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
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
