/*
 * WelsonJS.Toolkit: WelsonJS native component
 * 
 *     filename:
 *         BitmapUtils.cs
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
 */

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace WelsonJS
{
    public class BitmapUtils
    {
        private static Bitmap Load(string filename)
        {
            return new Bitmap(filename);
        }

        public static void Crop(string srcfile, string dstfile, int x, int y, int a, int b)
        {
            Bitmap originalBitmap = Load(srcfile);

            Rectangle cropArea = new Rectangle(x, y, a, b);
            Bitmap croppedBitmap = originalBitmap.Clone(cropArea, originalBitmap.PixelFormat);

            croppedBitmap.Save(dstfile);
        }

        public static int[] GetSize(string srcfile)
        {
            Bitmap bitmap = Load(srcfile);

            int width = bitmap.Width;
            int height = bitmap.Height;

            bitmap.Dispose();

            return new int[] { width, height };
        }

        public static int[] GetPixel(string srcfile, int x, int y)
        {
            Bitmap bitmap = Load(srcfile);

            Color pixelColor = bitmap.GetPixel(x, y);
            int red = pixelColor.R;
            int green = pixelColor.G;
            int blue = pixelColor.B;

            bitmap.Dispose();

            return new int[] { red, green, blue };
        }

        public static string GetBase64(string srcfile)
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
    }
}
