// MessageBoxTraceListener.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: 2026 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System.Diagnostics;
using System.Windows.Forms;

namespace WelsonJS.Launcher
{
    public class MessageBoxTraceListener : TraceListener
    {
        public override void Write(string message)
        {
            // nothing to do
        }

        public override void WriteLine(string message)
        {
            // nothing to do
        }

        public override void TraceEvent(
            TraceEventCache eventCache,
            string source,
            TraceEventType eventType,
            int id,
            string message)
        {
            if (eventType == TraceEventType.Error)
            {
                MessageBox.Show(
                    message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
