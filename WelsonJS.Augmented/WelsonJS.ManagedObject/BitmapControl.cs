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
            private static Dictionary<string, string> dict = new Dictionary<string, string>();

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

        private static Bitmap Load(string filename)
        {
            return new Bitmap(filename);
        }

        private static void Crop(string srcfile, string dstfile, int x, int y, int a, int b)
        {
            Bitmap originalBitmap = Load(srcfile);

            Rectangle cropArea = new Rectangle(x, y, a, b);
            Bitmap croppedBitmap = originalBitmap.Clone(cropArea, originalBitmap.PixelFormat);

            croppedBitmap.Save(dstfile);
        }

        private static int[] GetSize(string srcfile)
        {
            Bitmap bitmap = Load(srcfile);

            int width = bitmap.Width;
            int height = bitmap.Height;

            bitmap.Dispose();

            return new int[] { width, height };
        }

        private static int[] GetPixel(string srcfile, int x, int y)
        {
            Bitmap bitmap = Load(srcfile);

            Color pixelColor = bitmap.GetPixel(x, y);
            int red = pixelColor.R;
            int green = pixelColor.G;
            int blue = pixelColor.B;

            bitmap.Dispose();

            return new int[] { red, green, blue };
        }

        private static string GetBase64(string srcfile)
        {
            Bitmap bitmap = Load(srcfile);
            MemoryStream memoryStream = new MemoryStream();

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

            bitmap.Save(memoryStream, imageFormat);
            byte[] imageBytes = memoryStream.ToArray();
            string base64String = Convert.ToBase64String(imageBytes);

            bitmap.Dispose();
            memoryStream.Dispose();

            return base64String;
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
