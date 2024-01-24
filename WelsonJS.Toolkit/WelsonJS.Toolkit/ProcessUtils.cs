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
