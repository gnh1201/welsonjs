// ScreenMatching.cs
// https://github.com/gnh1201/welsonjs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;
using WelsonJS.Service;

public class ScreenMatch
{
    // User32.dll API 함수 선언
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hDestDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

    // https://stackoverflow.com/questions/60872044/how-to-get-scaling-factor-for-each-monitor-e-g-1-1-25-1-5
    [StructLayout(LayoutKind.Sequential)]
    private struct DEVMODE
    {
        private const int CCHDEVICENAME = 0x20;
        private const int CCHFORMNAME = 0x20;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public ScreenOrientation dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

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
    private string templateDirectoryPath;
    private string outputDirectoryPath;
    private int templateCurrentIndex = 0;
    private double threshold = 0.4;
    private string mode;
    private List<string> _params = new List<string>();
    private bool isSearchFromEnd = false;
    private bool isConvertToBinary = false;
    private byte thresholdConvertToBinary = 255;
    private bool isSaveToFile = false;

    public ScreenMatch(ServiceBase parent, string workingDirectory)
    {
        this.parent = (ServiceMain)parent;
        templateDirectoryPath = Path.Combine(workingDirectory, "app/assets/img/_templates");
        outputDirectoryPath = Path.Combine(workingDirectory, "app/assets/img/_captured");
        templateImages = new List<Bitmap>();

        // Read values from configration file
        string screen_time_mode;
        string screen_time_params;
        try
        {
            screen_time_mode = this.parent.GetSettingsFileHandler().Read("SCREEN_TIME_MODE", "Service");
            screen_time_params = this.parent.GetSettingsFileHandler().Read("SCREEN_TIME_PARAMS", "Service");
        }
        catch (Exception ex)
        {
            screen_time_mode = null;
            screen_time_params = null;
            this.parent.Log($"Failed to read from configration file: {ex.Message}");
        }

        if (!String.IsNullOrEmpty(screen_time_params))
        {
            string[] ss = screen_time_params.Split(',');
            foreach (string s in ss) {
                AddParam(s);
            }
        }
        
        if (_params.Contains("backward"))
        {
            isSearchFromEnd = true;
            this.parent.Log("Use the backward search when screen time");
        }

        if (_params.Contains("binary"))
        {
            isConvertToBinary = true;
            this.parent.Log("Use the convert to binary when screen time");
        }

        if (_params.Contains("save"))
        {
            isSaveToFile = true;
            this.parent.Log("Will be save an image file when capture the screens");
        }

        SetMode(screen_time_mode);
        LoadTemplateImages();
    }

    public void SetMode(string mode)
    {
        if (!String.IsNullOrEmpty(mode))
        {
            this.mode = mode;
        }
        else
        {
            this.mode = "screen";
        }
    }

    public void AddParam(string _param)
    {
        _params.Add(_param);
    }

    public void SetThreshold(double threshold)
    {
        this.threshold = threshold;
    }

    public void LoadTemplateImages()
    {
        string[] files;

        try
        {
            files = Directory.GetFiles(templateDirectoryPath, "*.png");
        }
        catch (Exception ex)
        {
            files = new string[]{};
            parent.Log($"Failed to read the directory structure: {ex.Message}");
        }

        foreach (var file in files)
        {
            Bitmap bitmap = new Bitmap(file)
            {
                Tag = Path.GetFileName(file)
            };
            templateImages.Add(!isConvertToBinary ?
                bitmap : ConvertToBinary(bitmap, thresholdConvertToBinary));
        }
    }

    // 캡쳐 및 템플릿 매칭 진행
    public List<ScreenMatchResult> CaptureAndMatch()
    {
        switch (mode)
        {
            case "screen":    // 화면 기준
                return CaptureAndMatchAllScreens();

            case "window":    // 윈도우 핸들 기준
                return CaptureAndMatchAllWindows();

            default:
                parent.Log($"Unknown capture mode: {mode}");
                break;
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

            if (_params.Contains("save"))
            {
                string outputFilePath = Path.Combine(outputDirectoryPath, $"{DateTime.Now.ToString("yyyy MM dd hh mm ss")}.png");
                mainImage.Save(outputFilePath);
                parent.Log($"Screenshot saved: {outputFilePath}");
            }

            Bitmap image = templateImages[templateCurrentIndex];
            parent.Log($"Trying match the template {image.Tag as string} on the screen {i}...");

            Point matchPosition = FindTemplate(mainImage, (Bitmap)image.Clone(), out double maxCorrelation);
            results.Add(new ScreenMatchResult
            {
                FileName = image.Tag.ToString(),
                ScreenNumber = i,
                Position = matchPosition,
                MaxCorrelation = maxCorrelation
            });
        }

        templateCurrentIndex = ++templateCurrentIndex % templateImages.Count;

        return results;
    }

    public Bitmap CaptureScreen(Screen screen)
    {
        Rectangle screenSize = screen.Bounds;

        DEVMODE dm = new DEVMODE();
        dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
        EnumDisplaySettings(screen.DeviceName, -1, ref dm);

        var scalingFactor = Math.Round(Decimal.Divide(dm.dmPelsWidth, screen.Bounds.Width), 2);
        int adjustedWidth = (int)(screenSize.Width * scalingFactor);
        int adjustedHeight = (int)(screenSize.Height * scalingFactor);

        Bitmap bitmap = new Bitmap(adjustedWidth, adjustedHeight);
        using (Graphics bitmapGraphics = Graphics.FromImage(bitmap))
        {
            bitmapGraphics.CopyFromScreen(screenSize.Left, screenSize.Top, 0, 0, new Size(adjustedWidth, adjustedHeight));
        }

        return !isConvertToBinary ?
            bitmap : ConvertToBinary(bitmap, thresholdConvertToBinary);
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
                    string processName = GetProcessName(hWnd);
                    GetWindowRect(hWnd, out RECT windowRect);
                    Point windowPosition = new Point(windowRect.Left, windowRect.Top); // 창 위치 계산
                    Bitmap windowImage = CaptureWindow(hWnd);

                    if (windowImage != null)
                    {
                        Bitmap image = templateImages[templateCurrentIndex];
                        Point matchPosition = FindTemplate(windowImage, image, out double maxCorrelation);
                        string templateFileName = image.Tag as string;

                        var result = new ScreenMatchResult
                        {
                            FileName = templateFileName,
                            WindowHandle = hWnd,
                            WindowTitle = windowTitle,
                            ProcessName = processName,
                            WindowPosition = windowPosition,
                            Position = matchPosition,
                            MaxCorrelation = maxCorrelation
                        };
                        results.Add(result);
                    }
                }
                catch { }
            }
            return true;
        }, IntPtr.Zero);

        templateCurrentIndex = ++templateCurrentIndex % templateImages.Count;

        return results;
    }

    public string GetWindowTitle(IntPtr hWnd)
    {
        int length = GetWindowTextLength(hWnd);
        StringBuilder sb = new StringBuilder(length + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public string GetProcessName(IntPtr hWnd)
    {
        uint processId;
        GetWindowThreadProcessId(hWnd, out processId);
        Process process = Process.GetProcessById((int)processId);
        return process.ProcessName;
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

        int startX = isSearchFromEnd ? mainWidth - templateWidth : 0;
        int endX = isSearchFromEnd ? -1 : mainWidth - templateWidth + 1;
        int stepX = isSearchFromEnd ? -1 : 1;

        int startY = isSearchFromEnd ? mainHeight - templateHeight : 0;
        int endY = isSearchFromEnd ? -1 : mainHeight - templateHeight + 1;
        int stepY = isSearchFromEnd ? -1 : 1;

        for (int x = startX; x != endX; x += stepX)
        {
            for (int y = startY; y != endY; y += stepY)
            {
                if (IsTemplateMatch(mainImage, templateImage, x, y, threshold))
                {
                    bestMatch = new Point(x, y);
                    maxCorrelation = 1.0;
                    return bestMatch;
                }
            }
        }

        return bestMatch;
    }

    private bool IsTemplateMatch(Bitmap mainImage, Bitmap templateImage, int offsetX, int offsetY, double threshold)
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

    private Bitmap ConvertToBinary(Bitmap image, byte threshold)
    {
        Bitmap binaryImage = new Bitmap(image.Width, image.Height);
        binaryImage.Tag = image.Tag;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                // Convert the pixel to grayscale
                Color pixelColor = image.GetPixel(x, y);
                byte grayValue = (byte)((pixelColor.R + pixelColor.G + pixelColor.B) / 3);

                // Apply threshold to convert to binary
                Color binaryColor = grayValue < threshold ? Color.Black : Color.White;
                binaryImage.SetPixel(x, y, binaryColor);
            }
        }

        return binaryImage;
    }
}