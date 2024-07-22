/*
 * WelsonJS.Toolkit: WelsonJS native component
 * 
 *     filename:
 *         ProcessUtils.cs
 * 
 *     description:
 *         WelsonJS - Build a Windows app on the Windows built-in JavaScript engine
 * 
 *     website:
 *         - https://github.com/gnh1201/welsonjs
 *         - https://catswords.social/@catswords_oss
 *         - https://teams.live.com/l/community/FEACHncAhq8ldnojAI
 * 
 *     author:
 *         Namhyeon Go <abuse@catswords.net>
 *
 *     license:
 *         GPLv3 or MS-RL(Microsoft Reciprocal License)
 * 
 */
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace WelsonJS
{
    public class ProcessUtils
    {
        public static List<int> ProcessIDs = new List<int>();

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
            if (string.IsNullOrEmpty(filepath))
            {
                filepath = OpenFileDialog();
                if (string.IsNullOrEmpty(filepath))
                {
                    return -1;
                }
            }

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = filepath;
                process.Start();

                int processId = process.Id;
                ProcessIDs.Add(processId);

                return processId;
            }
            catch
            {
                return -1;
            }
        }

        public static bool Close(int processId)
        {
            try
            {
                Process.GetProcessById(processId).CloseMainWindow();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
