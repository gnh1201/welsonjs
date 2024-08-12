// ScreenMatching.cs
// https://github.com/gnh1201/welsonjs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
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

    private ServiceMain parent;
    private List<Bitmap> templateImages;
    private string templateFolderPath;
    private int currentTemplateIndex = 0;
    private string captureMode;

    public ScreenMatching(ServiceBase parent, string workingDirectory)
    {
        this.parent = (ServiceMain)parent;
        templateFolderPath = Path.Combine(workingDirectory, "app/assets/img/_templates");
        templateImages = new List<Bitmap>();

        SetCaptureMode("screen");
        LoadTemplateImages();
    }

    public void SetCaptureMode(string captureMode)
    {
        this.captureMode = captureMode;
    }

    public void LoadTemplateImages()
    {
        string[] files;

        try
        {
            files = Directory.GetFiles(templateFolderPath, "*.png");
        }
        catch (Exception ex)
        {
            files = new string[] { };
            parent.Log($"Exception (ScreenMatching): {ex.Message}");
        }

        foreach (var file in files)
        {
            Bitmap bitmap = new Bitmap(file);
            bitmap.Tag = Path.GetFileName(file);
            templateImages.Add(bitmap);
        }
    }

    // 캡쳐 및 템플릿 매칭 진행
    public List<ScreenMatchResult> CaptureAndMatch()
    {
        switch(captureMode)
        {
            case "screen":    // 화면 기준
                return CaptureAndMatchAllScreens();

            case "windows":    // 윈도우 핸들 기준
                return CaptureAndMatchAllWindows();
        }

        return new List<ScreenMatchResult>();
    }

    // 화면을 기준으로 찾기
    public List<ScreenMatchResult> CaptureAndMatchAllScreens()
    {
        var results = new List<ScreenMatchResult>();

        for (int i = 0; i < Screen.AllScreens.Length; i++)
        {
            Screen screen = Screen.AllScreens[i];
            Bitmap mainImage = CaptureScreen(screen);

            Bitmap image = templateImages[currentTemplateIndex];
            parent.Log($"Trying match the template {image.Tag as string} on the screen {i}...");

            Point matchLocation = FindTemplate(mainImage, (Bitmap)image.Clone(), out double maxCorrelation);
            results.Add(new ScreenMatchResult
            {
                FileName = image.Tag.ToString(),
                ScreenNumber = i,
                Location = matchLocation,
                MaxCorrelation = maxCorrelation
            });
        }

        currentTemplateIndex = ++currentTemplateIndex % templateImages.Count;

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
                        Bitmap image = templateImages[currentTemplateIndex];
                        Point matchLocation = FindTemplate(windowImage, image, out double maxCorrelation);
                        string templateFileName = image.Tag as string;

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
                catch { }
            }
            return true;
        }, IntPtr.Zero);

        currentTemplateIndex = ++currentTemplateIndex % templateImages.Count;

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
                if (IsTemplateMatch(mainImage, templateImage, x, y, 0.8))   // matched 80% or above
                {
                    bestMatch = new Point(x, y);
                    maxCorrelation = 1;
                    return bestMatch;
                }
            }
        }

        return bestMatch;
    }

    private bool IsTemplateMatch(Bitmap mainImage, Bitmap templateImage, int offsetX, int offsetY, double threshold = 1.0)
    {
        int templateWidth = templateImage.Width;
        int templateHeight = templateImage.Height;
        int totalPixels = templateWidth * templateHeight;
        int requiredMatches = (int)(totalPixels * threshold);
        int matchedCount = 0;
        Random rand = new Random();

        while (matchedCount < requiredMatches)
        {
            int x = rand.Next(templateWidth);
            int y = rand.Next(templateHeight);
            Point point = new Point(x, y);

            if (mainImage.GetPixel(x + offsetX, y + offsetY) != templateImage.GetPixel(x, y))
            {
                return false;
            }

            matchedCount++;
        }

        return true;
    }
}