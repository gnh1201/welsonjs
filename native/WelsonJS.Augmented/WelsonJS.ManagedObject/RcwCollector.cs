// RcwCollector.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Namhyeon Go, Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
//
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace WelsonJS.ManagedObject
{
    /// <summary>
    /// Releases COM objects held by runtime callable wrappers (RCWs) in a
    /// timely manner.
    /// </summary>
    /// <remarks>
    /// Whenever a COM object passes through managed code (created, returned
    /// from a call, or received as an argument), the CLR wraps it in an RCW
    /// that holds a COM reference until the RCW is garbage collected. RCWs are
    /// tiny, and the memory owned by the COM object is invisible to the GC, so
    /// in a script host that allocates little managed memory the GC may not
    /// run for a long time. COM objects released by the script then stay alive
    /// (for example, 300 MSXML documents kept ~165 MB and ~900 handles).
    ///
    /// Releasing RCWs directly with Marshal.ReleaseComObject is unsafe here:
    /// the CLR shares one RCW per COM object, so a wrapper created later by
    /// Wrap() could be invalidated. Instead, a full collection is triggered
    /// after a number of COM objects have passed through, which only frees
    /// RCWs that are no longer referenced.
    /// </remarks>
    internal static class RcwCollector
    {
        public const int DefaultThreshold = 100;

        private static int _threshold = DefaultThreshold;
        private static int _count;

        /// <summary>
        /// Number of COM objects passing through managed code after which a
        /// collection runs. 0 disables automatic collection.
        /// </summary>
        public static int Threshold
        {
            get { return Volatile.Read(ref _threshold); }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "The threshold must be 0 (disabled) or greater.");

                Volatile.Write(ref _threshold, value);
            }
        }

        /// <summary>Counts <paramref name="value"/> if it is a COM object.</summary>
        public static void Observe(object value)
        {
            if (value == null || !Marshal.IsComObject(value))
                return;

            int threshold = Threshold;
            if (threshold <= 0)
                return;

            if (Interlocked.Increment(ref _count) >= threshold)
                Collect();
        }

        public static void Observe(object[] values)
        {
            if (values == null)
                return;

            for (int i = 0; i < values.Length; i++)
                Observe(values[i]);
        }

        /// <summary>
        /// Collects unreferenced RCWs so that their COM objects are released.
        /// </summary>
        public static void Collect()
        {
            Interlocked.Exchange(ref _count, 0);

            GC.Collect();
            // RCW cleanup runs through finalization; wait for it, then collect
            // the objects that became unreachable during finalization.
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
