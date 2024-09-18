// ScreenMatching.cs
// https://github.com/gnh1201/welsonjs
// https://github.com/gnh1201/welsonjs/wiki/Screen-Time-Feature
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using Tesseract;
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
    private bool busy = false;
    private List<string> _params = new List<string>();
    private bool isSearchFromEnd = false;
    private bool isSaveToFile = false;
    private bool isUseSampleClipboard = false;
    private bool isUseSampleOCR = false;
    private Size sampleSize;
    private int sampleAdjustX = 0;
    private int sampleAdjustY = 0;
    private List<string> sampleAny;
    private List<string> sampleNodup;
    private Queue<Bitmap> sampleOutdated;
    private byte thresholdConvertToBinary = 191;
    private string tesseractDataPath;
    private string tesseractLanguage;

    public ScreenMatch(ServiceBase parent, string workingDirectory)
    {
        this.parent = (ServiceMain)parent;

        templateDirectoryPath = Path.Combine(workingDirectory, "app/assets/img/_templates");
        outputDirectoryPath = Path.Combine(workingDirectory, "app/assets/img/_captured");

        templateImages = new List<Bitmap>();
        sampleSize = new Size
        {
            Width = 128,
            Height = 128
        };
        sampleOutdated = new Queue<Bitmap>();

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
            var screen_time_configs = screen_time_params
                .Split(',')
                .Select(pair => pair.Split('='))
                .ToDictionary(
                    parts => parts[0],
                    parts => parts.Length > 1 ? parts[1] : parts[0]
                );

            var config_keys = new string[]
            {
                "process_name",
                "sample_width",
                "sample_height",
                "sample_adjust_x",
                "sample_adjust_y",
                "sample_any",
                "sample_nodup",
                "backward",
                "save",
                "sample_clipboard",
                "sample_ocr"
            };

            foreach (var config_key in config_keys)
            {
                string config_value;
                screen_time_configs.TryGetValue(config_key, out config_value);

                if (config_value != null)
                {
                    switch (config_key)
                    {
                        case "backward":
                            {
                                isSearchFromEnd = true;
                                this.parent.Log("Use the backward search when screen time");
                                break;
                            }

                        case "save":
                            {
                                isSaveToFile = true;
                                this.parent.Log("Will be save an image file when capture the screens");
                                break;
                            }

                        case "sample_clipboard":
                            {
                                isUseSampleClipboard = true;
                                this.parent.Log($"Use Clipboard within a {sampleSize.Width}x{sampleSize.Height} pixel range around specific coordinates.");
                                break;
                            }

                        case "sample_ocr":
                            {
                                tesseractDataPath = Path.Combine(workingDirectory, "app/assets/tessdata_best");
                                tesseractLanguage = "eng";
                                isUseSampleOCR = true;
                                this.parent.Log($"Use OCR within a {sampleSize.Width}x{sampleSize.Height} pixel range around specific coordinates.");
                                break;
                            }

                        case "sample_width":
                            {
                                int.TryParse(config_value, out int w);
                                sampleSize.Width = w;
                                break;
                            }

                        case "sample_height":
                            {
                                int.TryParse(config_value, out int h);
                                sampleSize.Height = h;
                                break;
                            }

                        case "sample_adjust_x":
                            {
                                int.TryParse(config_value, out sampleAdjustX);
                                break;
                            }

                        case "sample_adjust_y":
                            {
                                int.TryParse(config_value, out sampleAdjustY);
                                break;
                            }

                        case "sample_any":
                            {
                                sampleAny = new List<string>(config_value.Split(':'));
                                break;
                            }

                        case "sample_nodup":
                            {
                                sampleNodup = new List<string>(config_value.Split(':'));
                                break;
                            }
                    }
                }
            }
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
            string filename = Path.GetFileName(file);
            Bitmap bitmap = new Bitmap(file)
            {
                Tag = filename
            };

            if (filename.StartsWith("binary_"))
            {
                templateImages.Add(ConvertToBinary(bitmap, thresholdConvertToBinary));
            }
            else
            {
                templateImages.Add(bitmap);
            }
        }
    }

    // 캡쳐 및 템플릿 매칭 진행
    public List<ScreenMatchResult> CaptureAndMatch()
    {
        List<ScreenMatchResult> results = new List<ScreenMatchResult>();

        if (busy)
        {
            throw new Exception("Waiting done a previous job...");
        }

        if (templateImages.Count > 0)
        {
            toggleBusy();

            switch (mode)
            {
                case "screen":    // 화면 기준
                    results = CaptureAndMatchAllScreens();
                    toggleBusy();
                    break;

                case "window":    // 윈도우 핸들 기준
                    results = CaptureAndMatchAllWindows();
                    toggleBusy();
                    break;

                default:
                    toggleBusy();
                    throw new Exception($"Unknown capture mode {mode}");
            }
        }

        return results;
    }

    // 화면을 기준으로 찾기
    public List<ScreenMatchResult> CaptureAndMatchAllScreens()
    {
        var results = new List<ScreenMatchResult>();

        for (int i = 0; i < Screen.AllScreens.Length; i++)
        {
            Screen screen = Screen.AllScreens[i];
            Bitmap mainImage = CaptureScreen(screen);

            if (isSaveToFile)
            {
                string outputFilePath = Path.Combine(outputDirectoryPath, $"{DateTime.Now.ToString("yyyy-MM-dd hh mm ss")}.png");
                ((Bitmap)mainImage.Clone()).Save(outputFilePath);
                parent.Log($"Screenshot saved: {outputFilePath}");
            }

            Bitmap image = templateImages[templateCurrentIndex];
            parent.Log($"Trying match the template {image.Tag as string} on the screen {i}...");

            string templateName = image.Tag as string;
            Size templateSize = new Size
            {
                Width = image.Width,
                Height = image.Height
            };

            Bitmap _mainImage;
            if (templateName.StartsWith("binary_"))
            {
                _mainImage = ConvertToBinary((Bitmap)mainImage.Clone(), thresholdConvertToBinary);
            }
            else
            {
                _mainImage = mainImage;
            }

            List<Point> matchPositions = FindTemplate(_mainImage, (Bitmap)image.Clone());
            matchPositions.ForEach((matchPosition) =>
            {
                try
                {
                    string text = sampleAny.Contains(templateName) ?
                        InspectSample((Bitmap)mainImage.Clone(), matchPosition, templateSize, templateName, sampleSize) : string.Empty;

                    results.Add(new ScreenMatchResult
                    {
                        FileName = image.Tag.ToString(),
                        ScreenNumber = i,
                        Position = matchPosition,
                        Text = text
                    });
                }
                catch (Exception ex)
                {
                    parent.Log($"Ignore the match. {ex.Message}");
                }
            });
        }

        if (results.Count > 0)
        {
            parent.Log("Match found");
        }
        else
        {
            parent.Log($"No match found");
        }

        templateCurrentIndex = ++templateCurrentIndex % templateImages.Count;

        return results;
    }

    public string InspectSample(Bitmap bitmap, Point matchPosition, Size templateSize, string templateName, Size sampleSize)
    {
        if (bitmap == null)
        {
            throw new ArgumentNullException(nameof(bitmap), "Bitmap cannot be null.");
        }

        if (matchPosition == null || matchPosition == Point.Empty)
        {
            throw new ArgumentException("matchPosition cannot be empty.");
        }

        // initial text
        string text = "";

        // Adjust coordinates
        int positionX = matchPosition.X + (templateSize.Width / 2);
        int positionY = matchPosition.Y + (templateSize.Height / 2);

        // Set range of crop image
        int cropX = Math.Max((positionX - sampleSize.Width / 2) + sampleAdjustX, 0);
        int cropY = Math.Max((positionY - sampleSize.Height / 2) + sampleAdjustY, 0);
        int cropWidth = Math.Min(sampleSize.Width, bitmap.Width - cropX);
        int cropHeight = Math.Min(sampleSize.Height, bitmap.Height - cropY);
        Rectangle cropArea = new Rectangle(cropX, cropY, cropWidth, cropHeight);

        // Crop image
        Bitmap croppedBitmap = bitmap.Clone(cropArea, bitmap.PixelFormat);

        // Save to the outdated samples
        if (sampleNodup.Contains(templateName))
        {
            int croppedBitmapHash = croppedBitmap.GetHashCode();
            bool bitmapExists = sampleOutdated.Any(x => x.GetHashCode() == croppedBitmapHash);

            if (bitmapExists)
            {
                throw new InvalidOperationException($"This may be a duplicate request. {templateName}");
            }
            else
            {
                sampleOutdated.Enqueue((Bitmap)croppedBitmap.Clone());
                parent.Log($"Added to the image queue. {templateName}");
            }
        }

        // if use Clipboard
        if (isUseSampleClipboard)
        {
            Thread th = new Thread(new ThreadStart(() =>
            {
                try
                {
                    Clipboard.SetImage((Bitmap)croppedBitmap.Clone());
                    parent.Log($"Copied the image to Clipboard");
                }
                catch (Exception ex)
                {
                    parent.Log($"Failed to copy to the clipboard: {ex.Message}");
                }
            }));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        // if use OCR
        if (isUseSampleOCR)
        {
            try
            {
                using (var engine = new TesseractEngine(tesseractDataPath, tesseractLanguage, EngineMode.Default))
                {
                    using (var page = engine.Process(croppedBitmap))
                    {
                        text = page.GetText();

                        parent.Log($"Mean confidence: {page.GetMeanConfidence()}");
                        parent.Log($"Text (GetText): {text}");
                    }
                }
            }
            catch (Exception ex)
            {
                parent.Log($"Failed to OCR: {ex.Message}");
            }
        }

        return text;
    }

    public Bitmap CaptureScreen(Screen screen)
    {
        Rectangle screenSize = screen.Bounds;

        DEVMODE dm = new DEVMODE();
        dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
        EnumDisplaySettings(screen.DeviceName, -1, ref dm);

        var scalingFactor = Math.Round(Decimal.Divide(dm.dmPelsWidth, screen.Bounds.Width), 2);
        parent.Log($"Resolved the screen scale: {scalingFactor}");

        int adjustedWidth = (int)(screenSize.Width * scalingFactor);
        int adjustedHeight = (int)(screenSize.Height * scalingFactor);

        Bitmap bitmap = new Bitmap(adjustedWidth, adjustedHeight);
        using (Graphics bitmapGraphics = Graphics.FromImage(bitmap))
        {
            bitmapGraphics.CopyFromScreen(screenSize.Left, screenSize.Top, 0, 0, new Size(adjustedWidth, adjustedHeight));
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
                    string processName = GetProcessName(hWnd);
                    GetWindowRect(hWnd, out RECT windowRect);
                    Point windowPosition = new Point(windowRect.Left, windowRect.Top);
                    Bitmap windowImage = CaptureWindow(hWnd);

                    if (windowImage != null)
                    {
                        Bitmap image = templateImages[templateCurrentIndex];
                        List<Point> matchPositions = FindTemplate(windowImage, image);
                        matchPositions.ForEach((matchPosition) =>
                        {
                            string templateName = image.Tag as string;
                            results.Add(new ScreenMatchResult
                            {
                                FileName = templateName,
                                WindowHandle = hWnd,
                                WindowTitle = windowTitle,
                                ProcessName = processName,
                                WindowPosition = windowPosition,
                                Position = matchPosition
                            });
                        });
                    }
                }
                catch (Exception ex) {
                    parent.Log($"Error {ex.Message}");
                }
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

    public List<Point> FindTemplate(Bitmap mainImage, Bitmap templateImage)
    {
        var matches = new List<Point>();

        int mainWidth = mainImage.Width;
        int mainHeight = mainImage.Height;
        int templateWidth = templateImage.Width;
        int templateHeight = templateImage.Height;

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
                    matches.Add(new Point(x, y));
                }
            }
        }

        return matches;
    }

    private void toggleBusy()
    {
        busy = !busy;
    }

    private bool IsTemplateMatch(Bitmap mainImage, Bitmap templateImage, int offsetX, int offsetY, double threshold)
    {
        int templateWidth = templateImage.Width;
        int templateHeight = templateImage.Height;
        int totalPixels = templateWidth * templateHeight;
        int requiredMatches = (int)(totalPixels * threshold);

        // When the square root of the canvas size of the image to be matched is less than 10, a complete match is applied.
        if (Math.Sqrt(templateWidth * templateHeight) < 10.0)
        {
            for (int y = 0; y < templateHeight; y++)
            {
                for (int x = 0; x < templateWidth; x++)
                {
                    if (mainImage.GetPixel(x + offsetX, y + offsetY) != templateImage.GetPixel(x, y))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // Otherwise, randomness is used.
        int matchedCount = 0;
        Random rand = new Random();
        while (matchedCount < requiredMatches)
        {
            int x = rand.Next(templateWidth);
            int y = rand.Next(templateHeight);

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
        if (image.Tag != null)
        {
            binaryImage.Tag = image.Tag;
        }

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                // Convert the pixel to grayscale
                Color pixelColor = image.GetPixel(x, y);
                byte grayValue = (byte)((pixelColor.R + pixelColor.G + pixelColor.B) / 3);

                // Apply threshold to convert to binary
                Color binaryColor = grayValue >= threshold ? Color.White : Color.Black;
                binaryImage.SetPixel(x, y, binaryColor);
            }
        }

        return binaryImage;
    }
}