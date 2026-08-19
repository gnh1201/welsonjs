using System;
using System.Runtime.InteropServices;
using System.Text;
using WelsonJS.ManagedObject.External;

namespace WelsonJS.ManagedObject
{
    [ComVisible(true)]
    [Guid("9d91ac4e-f26a-4d84-af81-3098e46d6185")]
    [ProgId("WelsonJS.MsCompress")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class MsCompress
    {
        public string Compress(string data)
        {
            byte[] input = Encoding.UTF8.GetBytes(data);
            byte[] compressed = SZDDComp.Compress(input);
            return Convert.ToBase64String(compressed);
        }

        public bool CompressFile(string src, string dst)
        {
            try
            {
                SZDDComp.Compress(src, dst);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string Decompress(string compressedData)
        {
            byte[] compressed = Convert.FromBase64String(compressedData);
            byte[] decompressed = SZDDComp.Decompress(compressed);
            return Encoding.UTF8.GetString(decompressed);
        }

        public bool DecompressFile(string src, string dst)
        {
            try
            {
                SZDDComp.Decompress(src, dst);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
