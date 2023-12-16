/*
 * WelsonJS.Toolkit: WelsonJS dotNET native component
 * 
 *     filename:
 *         Toolkit.cs
 * 
 *     description:
 *         WelsonJS - Build a Windows app on the Windows built-in JavaScript engine
 * 
 *     website:
 *         - https://github.com/gnh1201/welsonjs
 *         - https://catswords.social/@catswords_oss
 * 
 *     author:
 *         Namhyeon Go <abuse@catswords.net>
 *
 *     license:
 *         GPLv3 or MS-RL(Microsoft Reciprocal License)
 * 
 *     references:
 *         - https://stackoverflow.com/questions/9004352/call-a-function-in-a-console-app-from-vbscript
 *         - https://stackoverflow.com/questions/9501022/cannot-create-an-object-from-a-active-x-component
 *         - https://stackoverflow.com/questions/13547639/return-window-handle-by-its-name-title
 *         - https://blog.naver.com/zlatmgpdjtiq/222016292758
 *         - https://stackoverflow.com/questions/5427020/prompt-dialog-in-windows-forms
 *         - https://stackoverflow.com/questions/31856473/how-to-send-an-enter-press-to-another-application-in-wpf
 *         - https://stackoverflow.com/questions/11365605/c-sharp-postmessage-syntax-trying-to-post-a-wm-char-to-another-applications-win
 *         - https://docs.microsoft.com/ko-kr/windows/win32/inputdev/virtual-key-codes?redirectedfrom=MSDN
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WelsonJS
{
    [ComVisible(true)]
    public class Toolkit
    {
        private Dictionary<string, NamedSharedMemory> sharedMemoryDict;

        public static readonly string ApplicationName = "WelsonJS";

        public Toolkit()
        {
            sharedMemoryDict = new Dictionary<string, NamedSharedMemory>();
        }

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
            string result = WelsonJS.Prompt.ShowDialog(message, ApplicationName);
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
        public bool OpenNamedSharedMemory(string lpName)
        {
            NamedSharedMemory sharedMemory = new NamedSharedMemory(lpName);
            sharedMemoryDict.Add(lpName, sharedMemory);
            return sharedMemory.IsInitialized();
        }

        [ComVisible(true)]
        public NamedSharedMemory GetSharedMemory(string lpName)
        {
            if (sharedMemoryDict.ContainsKey(lpName))
            {
                NamedSharedMemory sharedMemory = sharedMemoryDict[lpName];
                if (sharedMemory.IsInitialized())
                {
                    return sharedMemory;
                }
            }

            return null;
        }

        [ComVisible(true)]
        public void CloseNamedSharedMemory(string lpName)
        {
            NamedSharedMemory sharedMemory = GetSharedMemory(lpName);
            if (sharedMemory != null)
            {
                sharedMemory.Close();
                sharedMemoryDict.Remove(lpName);
            }
        }

        [ComVisible(true)]
        public string ReadTextFromSharedMemory(string lpName)
        {
            NamedSharedMemory sharedMemory = GetSharedMemory(lpName);
            if (sharedMemory != null)
            {
                return sharedMemory.ReadText();
            }

            return "";
        }

        [ComVisible(true)]
        public void WriteTextToSharedMemory(string lpName, string text)
        {
            NamedSharedMemory sharedMemory = GetSharedMemory(lpName);
            if (sharedMemory != null)
            {
                sharedMemory.WriteText(text);
            }
        }

        [ComVisible(true)]
        public void ClearSharedMemory(string lpName)
        {
            NamedSharedMemory sharedMemory = GetSharedMemory(lpName);
            if (sharedMemory != null)
            {
                sharedMemory.Clear();
            }
        }
    }
}
