using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

// https://github.com/niklyadov/tiny-ini-file-class
namespace WelsonJS.Service.TinyINIController
{
    class IniFile
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string section, string key, string value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string section, string key, string Default, StringBuilder RetVal, int Size, string FilePath);

        private readonly FileInfo FileInfo;

        private readonly string exe = Assembly.GetExecutingAssembly().GetName().Name;

        private readonly FileAccess fileAccess;

        public IniFile(string path = null, FileAccess access = FileAccess.ReadWrite)
        {
            fileAccess = access;
            FileInfo = new FileInfo(path ?? exe);
        }

        public string Read(string key, string section = null)
        {
            var RetVal = new StringBuilder(65025);

            if (fileAccess != FileAccess.Write)
            {
                GetPrivateProfileString(section ?? exe, key, "", RetVal, 65025, FileInfo.FullName);
            }
            else
            {
                throw new Exception("Can`t read file! No access!");
            }

            return RetVal.ToString();
        }
        public void Write(string key, string value, string section = null)
        {
            if (fileAccess != FileAccess.Read)
            {
                WritePrivateProfileString(section ?? exe, key, value, FileInfo.FullName);
            }
            else
            {
                throw new Exception("Can`t write to file! No access!");
            }
        }

        public void DeleteKey(string key, string section = null)
        {
            Write(key, null, section ?? exe);
        }

        public void DeleteSection(string section = null)
        {
            Write(null, null, section ?? exe);
        }

        public bool KeyExists(string key, string section = null)
        {
            return Read(key, section).Length > 0;
        }
    }
}
