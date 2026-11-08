using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace WelsonJS.ManagedObject
{
    [ComVisible(true)]
    [Guid("4ea39f55-df2a-41af-8beb-0bd22dea5e65")]
    [ProgId("WelsonJS.BitmapControl")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class BitmapControl
    {
        private class Serializer
        {
            private readonly Dictionary<string, string> dict = new Dictionary<string, string>();

            public void Add(string key, string value)
            {
                dict[key] = value;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                foreach (var x in dict)
                {
                    sb.Append($"{x.Key}={x.Value}; ");
                }
                if (sb.Length > 0) sb.Length -= 2;

                return sb.ToString();
            }
        }

        private Bitmap Load(string filename)
        {
            return new Bitmap(filename);
        }

        private void Crop(string srcfile, string dstfile, int x, int y, int a, int b)
        {
            using (Bitmap originalBitmap = Load(srcfile))
            {
                Rectangle cropArea = new Rectangle(x, y, a, b);
                using (Bitmap croppedBitmap = originalBitmap.Clone(cropArea, originalBitmap.PixelFormat))
                {
                    // Dispose the source first so that dstfile may equal srcfile.
                    originalBitmap.Dispose();
                    croppedBitmap.Save(dstfile);
                }
            }
        }

        private int[] GetSize(string srcfile)
        {
            using (Bitmap bitmap = Load(srcfile))
            {
                return new int[] { bitmap.Width, bitmap.Height };
            }
        }

        private int[] GetPixel(string srcfile, int x, int y)
        {
            using (Bitmap bitmap = Load(srcfile))
            {
                Color pixelColor = bitmap.GetPixel(x, y);
                return new int[] { pixelColor.R, pixelColor.G, pixelColor.B };
            }
        }

        private string GetBase64(string srcfile)
        {
            ImageFormat imageFormat;
            if (srcfile.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
            {
                imageFormat = ImageFormat.Bmp;
            }
            else if (srcfile.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || srcfile.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                imageFormat = ImageFormat.Jpeg;
            }
            else if (srcfile.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                imageFormat = ImageFormat.Png;
            }
            else if (srcfile.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase))
            {
                imageFormat = ImageFormat.Tiff;
            }
            else if (srcfile.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            {
                imageFormat = ImageFormat.Gif;
            }
            else
            {
                return "";
            }

            using (Bitmap bitmap = Load(srcfile))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, imageFormat);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        public string GetImageSize(string srcfile)
        {
            int[] result = GetSize(srcfile);

            var serializer = new Serializer();
            serializer.Add("width", result[0].ToString());
            serializer.Add("height", result[1].ToString());

            return serializer.ToString();
        }

        public string GetImagePixel(string srcfile, int x, int y)
        {
            int[] result = GetPixel(srcfile, x, y);

            var serializer = new Serializer();
            serializer.Add("red", result[0].ToString());
            serializer.Add("green", result[1].ToString());
            serializer.Add("blue", result[2].ToString());

            return serializer.ToString();
        }

        public string GetImageBase64(string srcfile)
        {
            return GetBase64(srcfile);
        }

        [ComVisible(true)]
        public void CropImage(string srcfile, string dstfile, int x, int y, int a, int b)
        {
            Crop(srcfile, dstfile, x, y, a, b);
        }
    }
}
