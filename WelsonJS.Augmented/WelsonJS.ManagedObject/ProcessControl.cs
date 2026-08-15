using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WelsonJS.ManagedObject
{
    [ComVisible(true)]
    [Guid("52d98d4e-4783-4d33-9ffe-3355da8df41d")]
    [ProgId("WelsonJS.ProcessControl")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ProcessControl
    {
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowEx(IntPtr hWnd1, IntPtr hWnd2, string lpsz1, string lpsz2);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam);

        public enum MessageType : int
        {
            WM_MOUSEMOVE = 0x200,
            WM_LBUTTONDOWN = 0x201, //Left mousebutton down
            WM_LBUTTONUP = 0x202,  //Left mousebutton up
            WM_LBUTTONDBLCLK = 0x203, //Left mousebutton doubleclick
            WM_RBUTTONDOWN = 0x204, //Right mousebutton down
            WM_RBUTTONUP = 0x205,   //Right mousebutton up
            WM_RBUTTONDBLCLK = 0x206, //Right mousebutton doubleclick
            WM_KEYDOWN = 0x100,  //Key down
            WM_KEYUP = 0x101,   //Key up
            WM_SYSKEYDOWN = 0x104,
            WM_SYSKEYUP = 0x105,
            WM_CHAR = 0x102,     //char
            WM_COMMAND = 0x111
        }

        private enum VirtualKey : int
        {
            VK_RETURN = 0x0D,
            VK_F1 = 0x70,
            VK_F2 = 0x71,
            VK_F3 = 0x72,
            VK_F4 = 0x73,
            VK_F5 = 0x74,
            VK_F6 = 0x75,
            VK_F7 = 0x76,
            VK_F8 = 0x77,
            VK_F9 = 0x78,
            VK_F10 = 0x79,
            VK_F11 = 0x7A,
            VK_F12 = 0x7B
        }

        private static List<Process> Processes = new List<Process>();

        private IntPtr GetWindowByTitleContains(string title)
        {
            IntPtr hWnd = IntPtr.Zero;

            foreach (var proc in Process.GetProcesses())
            {
                if (proc.MainWindowTitle.Contains(title))
                {
                    hWnd = proc.MainWindowHandle;
                    break;
                }
            }

            return hWnd;
        }

        public bool SendClick(string title, int x, int y)
        {
            IntPtr hWnd = GetWindowByTitleContains(title);
            if (hWnd != IntPtr.Zero)
            {
                PostMessage(hWnd, (int)MessageType.WM_LBUTTONDOWN, 1, new IntPtr(y * 0x10000 + x));
                PostMessage(hWnd, (int)MessageType.WM_LBUTTONUP, 0, new IntPtr(y * 0x10000 + x));
            }

            return hWnd != IntPtr.Zero;
        }

        public bool SendKey(string title, char key)
        {
            IntPtr hWnd = GetWindowByTitleContains(title);
            return SendKey(hWnd, key);
        }

        private bool SendKey(IntPtr hWnd, char key)
        {
            return PostMessage(hWnd, (int)MessageType.WM_CHAR, key, IntPtr.Zero);
        }

        public bool SendKeys(string title, string str)
        {
            IntPtr hWnd = GetWindowByTitleContains(title);
            if (hWnd != IntPtr.Zero)
            {
                foreach (char i in str) SendKey(hWnd, i);
                return true;
            }

            return false;
        }

        public bool SendFnKey(string title, int num)
        {
            char[] fnKeys = new char[]
            {
                (char)0x00,
                (char)VirtualKey.VK_F1,
                (char)VirtualKey.VK_F2,
                (char)VirtualKey.VK_F3,
                (char)VirtualKey.VK_F4,
                (char)VirtualKey.VK_F5,
                (char)VirtualKey.VK_F6,
                (char)VirtualKey.VK_F7,
                (char)VirtualKey.VK_F8,
                (char)VirtualKey.VK_F9,
                (char)VirtualKey.VK_F10,
                (char)VirtualKey.VK_F11,
                (char)VirtualKey.VK_F12
            };
            IntPtr hWnd = GetWindowByTitleContains(title);

            if (hWnd != IntPtr.Zero && (fnKeys.Length + 1 < num))
            {
                PostMessage(hWnd, (int)MessageType.WM_KEYDOWN, fnKeys[num], IntPtr.Zero);
                PostMessage(hWnd, (int)MessageType.WM_KEYUP, fnKeys[num], IntPtr.Zero);

                return true;
            }

            return false;
        }

        public bool SendEnterKey(string title)
        {
            IntPtr hWnd = GetWindowByTitleContains(title);

            if (hWnd != IntPtr.Zero)
            {
                PostMessage(hWnd, (int)MessageType.WM_KEYDOWN, (char)VirtualKey.VK_RETURN, IntPtr.Zero);
                PostMessage(hWnd, (int)MessageType.WM_KEYUP, (char)VirtualKey.VK_RETURN, IntPtr.Zero);
                return true;
            }

            return false;
        }

        public int Open(string filepath)
        {
            int processId = -1;

            if (string.IsNullOrEmpty(filepath))
            {
                filepath = new Dialog().OpenFileDialog();
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
                Processes.Add(process);
            }
            catch
            {
                processId = -1;
            }

            return processId;
        }

        public bool Close(int processId)
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
