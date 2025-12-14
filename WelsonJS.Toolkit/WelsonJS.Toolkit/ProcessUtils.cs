// ProcessUtils.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX - FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace WelsonJS
{
    public class ProcessUtils
    {
        public static List<Process> ProcessList = new List<Process>();

        public static string OpenFileDialog()
        {
            string filepath = string.Empty;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "All files (*.*)|*.*";
                openFileDialog.RestoreDirectory = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filepath = openFileDialog.FileName;
                }
            }

            return filepath;
        }

        public static int Open(string filepath)
        {
            int processId = -1;

            if (string.IsNullOrEmpty(filepath))
            {
                filepath = OpenFileDialog();
                if (string.IsNullOrEmpty(filepath))
                {
                    return processId;
                }
            }

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = filepath;
                process.Start();
                ProcessList.Add(process);
            }
            catch {
                processId = -1;
            }

            return processId;
        }

        public static bool Close(int processId)
        {
            try
            {
                Process.GetProcessById(processId).CloseMainWindow();
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
