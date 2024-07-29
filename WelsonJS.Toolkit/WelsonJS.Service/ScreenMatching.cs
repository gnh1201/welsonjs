using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WelsonJS.Service;

public class ScreenMatching
{
    // User32.dll API 함수 선언
    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);
    private const int SRCCOPY = 0x00CC0020;

    // 델리게이트 선언
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    // RECT 구조체 선언
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public List<Bitmap> templateImages;
    string templateFolderPath;

    public ScreenMatching(string workingDirectory)
    {
        templateFolderPath = Path.Combine(workingDirectory, "app/assets/img/_templates");
        templateImages = new List<Bitmap>();
        LoadTemplateImages();
    }

    public void LoadTemplateImages()
    {
        var files = System.IO.Directory.GetFiles(templateFolderPath, "*.png");

        foreach (var file in files)
        {
            Bitmap bitmap = new Bitmap(file);
            bitmap.Tag = System.IO.Path.GetFileName(file);
            templateImages.Add(bitmap);
        }
    }

    // 화면을 기준으로 찾기
    public List<ScreenMatchResult> CaptureAndMatchAllScreens()
    {
        var results = new List<ScreenMatchResult>();

        for (int i = 0; i < Screen.AllScreens.Length; i++)
        {
            Screen screen = Screen.AllScreens[i];
            Bitmap mainImage = CaptureScreen(screen);

            foreach (Bitmap templateImage in templateImages)
            {
                Point matchLocation = FindTemplate(mainImage, (Bitmap)templateImage.Clone(), out double maxCorrelation);

                results.Add(new ScreenMatchResult
                {
                    FileName = templateImage.Tag.ToString(),
                    ScreenNumber = i,
                    Location = matchLocation,
                    MaxCorrelation = maxCorrelation
                });
            }
        }

        return results;
    }

    public static Bitmap CaptureScreen(Screen screen)
    {
        Rectangle screenSize = screen.Bounds;
        Bitmap bitmap = new Bitmap(screenSize.Width, screenSize.Height);

        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(screenSize.Left, screenSize.Top, 0, 0, screenSize.Size);
        }

        return bitmap;
    }

    // 윈도우 핸들을 기준으로 찾기
    public List<ScreenMatchResult> CaptureAndMatchAllWindows()
    {
        var results = new List<ScreenMatchResult>();

        // 모든 윈도우 핸들을 열거
        EnumWindows((hWnd, lParam) =>
        {
            if (IsWindowVisible(hWnd))
            {
                try
                {
                    string windowTitle = GetWindowTitle(hWnd);
                    Bitmap windowImage = CaptureWindow(hWnd);
                    if (windowImage != null)
                    {
                        foreach (var templateImage in templateImages)
                        {
                            Point matchLocation = FindTemplate(windowImage, templateImage, out double maxCorrelation);
                            string templateFileName = templateImage.Tag as string;

                            var result = new ScreenMatchResult
                            {
                                FileName = templateFileName,
                                WindowHandle = hWnd,
                                WindowTitle = windowTitle,
                                Location = matchLocation,
                                MaxCorrelation = maxCorrelation
                            };
                            results.Add(result);
                        }
                    }
                }
                catch { }
            }
            return true;
        }, IntPtr.Zero);

        return results;
    }

    public string GetWindowTitle(IntPtr hWnd)
    {
        int length = GetWindowTextLength(hWnd);
        StringBuilder sb = new StringBuilder(length + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public Bitmap CaptureWindow(IntPtr hWnd)
    {
        GetWindowRect(hWnd, out RECT rect);
        int width = rect.Right - rect.Left;
        int height = rect.Bottom - rect.Top;

        if (width <= 0 || height <= 0)
            return null;

        Bitmap bitmap = new Bitmap(width, height);
        Graphics graphics = Graphics.FromImage(bitmap);
        IntPtr hDC = graphics.GetHdc();
        IntPtr windowDC = GetDC(hWnd);

        bool success = BitBlt(hDC, 0, 0, width, height, windowDC, 0, 0, SRCCOPY);
        ReleaseDC(hWnd, windowDC);
        graphics.ReleaseHdc(hDC);

        return success ? bitmap : null;
    }

    public Point FindTemplate(Bitmap mainImage, Bitmap templateImage, out double maxCorrelation)
    {
        int mainWidth = mainImage.Width;
        int mainHeight = mainImage.Height;
        int templateWidth = templateImage.Width;
        int templateHeight = templateImage.Height;

        Point bestMatch = Point.Empty;
        maxCorrelation = 0;

        for (int x = 0; x <= mainWidth - templateWidth; x++)
        {
            for (int y = 0; y <= mainHeight - templateHeight; y++)
            {
                if (IsTemplateMatch(mainImage, templateImage, x, y))
                {
                    bestMatch = new Point(x, y);
                    maxCorrelation = 1; // 완전 일치
                    return bestMatch;
                }
            }
        }

        return bestMatch;
    }

    private bool IsTemplateMatch(Bitmap mainImage, Bitmap templateImage, int offsetX, int offsetY)
    {
        int templateWidth = templateImage.Width;
        int templateHeight = templateImage.Height;

        for (int x = 0; x < templateWidth; x++)
        {
            for (int y = 0; y < templateHeight; y++)
            {
                if (mainImage.GetPixel(x + offsetX, y + offsetY) != templateImage.GetPixel(x, y))
                {
                    return false;
                }
            }
        }

        return true;
    }
}
