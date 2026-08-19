using System;
using System.Runtime.InteropServices;
using System.Text;
using WelsonJS.ManagedObject.External;

namespace WelsonJS.ManagedObject.Legacy
{
    [ComVisible(true)]
    [Guid("cc60a0c6-8e6b-4923-a54c-48d3674b8b15")]
    [ProgId("WelsonJS.Legacy.Toolkit")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class Toolkit
    {
        private static ProcessControl pc = new ProcessControl();
        private static Dialog dialog = new Dialog();
        private static BitmapControl bc = new BitmapControl();
        private static MsCompress mc = new MsCompress();

        public bool SendClick(string title, int x, int y)
        {
            return pc.SendClick(title, x, y);
        }

        public bool SendKey(string title, char key)
        {
            return pc.SendKey(title, key);
        }

        public bool SendKeys(string title, string str)
        {
            return pc.SendKeys(title, str);
        }

        public int Alert(string message)
        {
            return dialog.Alert(message);
        }

        public bool Confirm(string message)
        {
            return dialog.Confirm(message);
        }

        public string Prompt(string message, string _default = "")
        {
            return dialog.ShowDialog(message, _default);
        }

        public bool SendEnterKey(string title)
        {
            return pc.SendEnterKey(title);
        }

        public bool SendFnKey(string title, int num)
        {
            return pc.SendFnKey(title, num);
        }

        public bool WriteTextToSharedMemory(string lpName, string text)
        {
            return new NamedSharedMemory(lpName).WriteTextToSharedMemory(lpName, text);
        }

        [ComVisible(true)]
        public string ReadTextFromSharedMemory(string lpName)
        {
            return new NamedSharedMemory(lpName).ReadTextFromSharedMemory(lpName);
        }

        public bool ClearSharedMemory(string lpName)
        {
            return new NamedSharedMemory(lpName).ClearSharedMemory(lpName);
        }

        public bool CloseSharedMemory(string lpName)
        {
            return new NamedSharedMemory(lpName).CloseSharedMemory(lpName);
        }

        public int OpenProcess(string filepath)
        {
            return pc.Open(filepath);
        }

        public bool CloseProcess(int processId)
        {
            return pc.Close(processId);
        }

        public string CompressLZ77(string data)
        {
            return mc.Compress(data);
        }

        public string DecompressLZ77(string compressedData)
        {
            return mc.Decompress(compressedData);
        }

        public string EncryptString(string key, string data)
        {
            byte[] userKey = Encoding.ASCII.GetBytes(key);
            byte[] dataIn = Encoding.UTF8.GetBytes(data);

            Cipher.ECB cipher = new Cipher.ECB(userKey);
            return Convert.ToBase64String(cipher.Encrypt(dataIn));
        }

        public string DecryptString(string key, string encryptedData)
        {
            byte[] userKey = Encoding.ASCII.GetBytes(key);
            byte[] dataIn = Convert.FromBase64String(encryptedData);

            Cipher.ECB cipher = new Cipher.ECB(userKey);
            return Encoding.UTF8.GetString(cipher.Decrypt(dataIn)).Trim('\0');
        }

        public string GetImageSize(string srcfile)
        {
            return bc.GetImageSize(srcfile);
        }

        public string GetImagePixel(string srcfile, int x, int y)
        {
            return bc.GetImagePixel(srcfile, x, y);
        }

        public string GetImageBase64(string srcfile)
        {
            return bc.GetImageBase64(srcfile);
        }

        public void CropImage(string srcfile, string dstfile, int x, int y, int a, int b)
        {
            bc.CropImage(srcfile, dstfile, x, y, a, b);
        }
    }
}
