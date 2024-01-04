using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace WelsonJS.Toolkit.Experimental
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SharedMemoryListener listener = new SharedMemoryListener();

            Console.Write("Input the shared memory name: ");
            listener.memName = Console.ReadLine();

            Console.Write("Open the second process name: ");
            listener.processName = listener.OpenFileDialog();

            Thread listenerThread = new Thread(listener.Listen);
            listenerThread.Start();

            Process.Start(listener.processName);
        }

        class SharedMemoryListener
        {
            public string memName { get; set; }
            public string processName { get; set; }

            public void Listen()
            {
                NamedSharedMemory mem = new NamedSharedMemory(memName);
                Console.WriteLine("Listening the shared memory...");
                while (true)
                {
                    Console.WriteLine(mem.ReadText());
                    Thread.Sleep(100);
                }
            }

            public string OpenFileDialog()
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
        }
    }
}
