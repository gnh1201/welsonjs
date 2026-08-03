// Toolkit.cs
// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX - FileCopyrightText: 2025 Catswords OSS and WelsonJS Contributors
// https://github.com/gnh1201/welsonjs
// 
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WelsonJS.Toolkit
{
    [ComVisible(true)]
    [ProgId("WelsonJS.Toolkit")]
    public class Toolkit
    {
        public static readonly string ApplicationName = "WelsonJS";

        [ComVisible(true)]
        public bool SendClick(string title, int x, int y)
        {
            IntPtr hWnd = Window.GetWindowByTitleContains(title);
            if (hWnd != IntPtr.Zero) {
                Window.PostMessage(hWnd, (int)Window.Message.WM_LBUTTONDOWN, 1, new IntPtr(y * 0x10000 + x));
                Window.PostMessage(hWnd, (int)Window.Message.WM_LBUTTONUP, 0, new IntPtr(y * 0x10000 + x));
            }

            return hWnd != IntPtr.Zero;
        }

        [ComVisible(true)]
        public bool SendKey(string title, char key)
        {
            IntPtr hWnd = Window.GetWindowByTitleContains(title);
            return SendKey(hWnd, key);
        }

        // [ComVisible(false)]
        public bool SendKey(IntPtr hWnd, char key)
        {
            return Window.PostMessage(hWnd, (int)Window.Message.WM_CHAR, key, IntPtr.Zero);
        }

        [ComVisible(true)]
        public bool SendKeys(string title, string str)
        {
            IntPtr hWnd = Window.GetWindowByTitleContains(title);
            if (hWnd != IntPtr.Zero)
            {
                foreach (char i in str) SendKey(hWnd, i);
                return true;
            }

            return false;
        }

        [ComVisible(true)]
        public int Alert(string message)
        {
            MessageBox.Show(message, ApplicationName);
            return 0;
        }

        [ComVisible(true)]
        public bool Confirm(string message)
        {
            return (MessageBox.Show(message, ApplicationName, MessageBoxButtons.YesNo) == DialogResult.Yes);
        }

        [ComVisible(true)]
        public string Prompt(string message, string _default = "")
        {
            string result = WelsonJS.Toolkit.Prompt.ShowDialog(message, ApplicationName);
            return (result == "" ? _default : result);
        }

        [ComVisible(true)]
        public bool SendEnterKey(string title)
        {
            IntPtr hWnd = Window.GetWindowByTitleContains(title);

            if (hWnd != IntPtr.Zero)
            {
                Window.PostMessage(hWnd, (int)Window.Message.WM_KEYDOWN, (char)Window.VirtualKey.VK_RETURN, IntPtr.Zero);
                Window.PostMessage(hWnd, (int)Window.Message.WM_KEYUP, (char)Window.VirtualKey.VK_RETURN, IntPtr.Zero);
                return true;
            }

            return false;
        }

        [ComVisible(true)]
        public bool SendFnKey(string title, int num) {
            char[] fnKeys = new char[]
            {
                (char)0x00,
                (char)Window.VirtualKey.VK_F1,
                (char)Window.VirtualKey.VK_F2,
                (char)Window.VirtualKey.VK_F3,
                (char)Window.VirtualKey.VK_F4,
                (char)Window.VirtualKey.VK_F5,
                (char)Window.VirtualKey.VK_F6,
                (char)Window.VirtualKey.VK_F7,
                (char)Window.VirtualKey.VK_F8,
                (char)Window.VirtualKey.VK_F9,
                (char)Window.VirtualKey.VK_F10,
                (char)Window.VirtualKey.VK_F11,
                (char)Window.VirtualKey.VK_F12
            };
            IntPtr hWnd = Window.GetWindowByTitleContains(title);

            if (hWnd != IntPtr.Zero && (fnKeys.Length + 1 < num))
            {
                Window.PostMessage(hWnd, (int)Window.Message.WM_KEYDOWN, fnKeys[num], IntPtr.Zero);
                Window.PostMessage(hWnd, (int)Window.Message.WM_KEYUP, fnKeys[num], IntPtr.Zero);
                return true;
            }

            return false;
        }

        // [Toolkit] Access to a shared memory #96
        [ComVisible(true)]
        public bool WriteTextToSharedMemory(string lpName, string text)
        {
            NamedSharedMemory mem = new NamedSharedMemory(lpName);
            if (mem.IsInitialized())
            {
                return mem.WriteText(text);
            }

            return false;
        }

        [ComVisible(true)]
        public string ReadTextFromSharedMemory(string lpName)
        {
            NamedSharedMemory mem = new NamedSharedMemory(lpName);
            if (mem.IsInitialized()) {
                return mem.ReadText();
            }

            return "";
        }

        [ComVisible(true)]
        public bool ClearSharedMemory(string lpName)
        {
            NamedSharedMemory mem = new NamedSharedMemory(lpName);
            if (mem.IsInitialized())
            {
                return mem.Clear();
            }

            return false;
        }

        [ComVisible(true)]
        public bool CloseSharedMemory(string lpName)
        {
            NamedSharedMemory mem = new NamedSharedMemory(lpName);
            if (mem.IsInitialized())
            {
                return mem.Close();
            }

            return false;
        }

        [ComVisible(true)]
        public string GetFilePathFromDialog()
        {
            return ProcessKit.OpenFileDialog();
        }

        [ComVisible(true)]
        public int OpenProcess(string filepath)
        {
            return ProcessKit.Open(filepath);
        }

        [ComVisible(true)]
        public bool CloseProcess(int processID)
        {
            return ProcessKit.Close(processID);
        }

        [ComVisible(true)]
        public string GetImageSize(string srcfile)
        {
            int[] result = BitmapKit.GetSize(srcfile);

            var codec = new KeyValueCodec
            {
                { "width", result[0].ToString() },
                { "height", result[1].ToString() }
            };

            return codec.ToString();
        }

        [ComVisible(true)]
        public string GetImagePixel(string srcfile)
        {
            int[] result = BitmapKit.GetSize(srcfile);

            var codec = new KeyValueCodec
            {
                { "red", result[0].ToString() },
                { "green", result[1].ToString() },
                { "blue", result[2].ToString() }
            };

            return codec.ToString();
        }

        [ComVisible(true)]
        public string GetImageBase64(string srcfile)
        {
            return BitmapKit.GetBase64(srcfile);
        }

        [ComVisible(true)]
        public void CropImage(string srcfile, string dstfile, int x, int y, int a, int b)
        {
            BitmapKit.Crop(srcfile, dstfile, x, y, a, b);
        }
    }
}
