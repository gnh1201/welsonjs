// ScreenMatching.cs
// https://github.com/gnh1201/welsonjs
// https://catswords-oss.rdbl.io/5719744820/8803957194
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using Tesseract;
using WelsonJS.Service;
using Microsoft.Extensions.Logging;

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
    private ILogger logger;
    private List<Bitmap> templateImages;
    private string templateDirectoryPath;
    private string outputDirectoryPath;
    private int templateCurrentIndex = 0;
    private double threshold = 0.3;
    private string mode;
    private bool busy;
    private List<string> _params = new List<string>();
    private bool isSearchFromEnd = false;
    private bool isSaveToFile = false;
    private Size sampleSize;
    private int sampleAdjustX;
    private int sampleAdjustY;
    private List<string> sampleAny;
    private List<string> sampleClipboard;
    private List<string> sampleOcr;
    private List<string> sampleNodup;
    private Size sampleNodupSize;
    private Queue<Bitmap> outdatedSamples;
    private string tesseractDataPath;
    private string tesseractLanguage;

    private void SetBusy(bool busy)
    {
        this.busy = busy;
        logger.LogInformation($"State changed: busy={busy}");
    }

    private decimal GetScreenScalingFactor(Screen screen)
    {
        DEVMODE dm = new DEVMODE();
        dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
        EnumDisplaySettings(screen.DeviceName, -1, ref dm);

        decimal scalingFactor = Math.Round(Decimal.Divide(dm.dmPelsWidth, screen.Bounds.Width), 2);
        if (scalingFactor > 1)
        {
            logger.LogInformation($"Screen with scaling detected: {scalingFactor}x");
            logger.LogWarning("Please check the screen DPI.");
        }

        return scalingFactor;
    }

    public class TemplateInfo
    {
        public string FileName { get; set; }
        public int Index { get; set; }

        public TemplateInfo(string fileName, int index)
        {
            FileName = fileName;
            Index = index;
        }
    }

    public class SampleInfo
    {
        public string FileName { get; set; }
        public uint Crc32 { get; set; }

        public SampleInfo(string fileName, uint crc32)
        {
            FileName = fileName;
            Crc32 = crc32;
        }
    }

    public ScreenMatch(ServiceBase _parent, string workingDirectory, ILogger _logger)
    {
        parent = (ServiceMain)_parent;
        logger = _logger;

        SetBusy(false);

        templateDirectoryPath = Path.Combine(workingDirectory, "app/assets/img/_templates");
        outputDirectoryPath = Path.Combine(workingDirectory, "app/assets/img/_captured");
        templateImages = new List<Bitmap>();

        // Initialize variables for sampling process
        sampleSize = new Size
        {
            Width = 128,
            Height = 128
        };
        sampleAdjustX = 0;
        sampleAdjustY = 0;
        sampleAny = new List<string>();
        sampleClipboard = new List<string>();
        sampleOcr = new List<string>();
        sampleNodup = new List<string>();
        sampleNodupSize = new Size
        {
            Width = 180,
            Height = 60
        };
        outdatedSamples = new Queue<Bitmap>();

        // Read values from configration file
        string screen_time_mode;
        string screen_time_params;
        try
        {
            screen_time_mode = parent.ReadSettingsValue("SCREEN_TIME_MODE");
            screen_time_params = parent.ReadSettingsValue("SCREEN_TIME_PARAMS");
        }
        catch (Exception ex)
        {
            screen_time_mode = null;
            screen_time_params = null;
            logger.LogInformation($"Failed to read from configration file: {ex.Message}");
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
                                logger.LogInformation("Use the backward search when screen time");
                                break;
                            }

                        case "save":
                            {
                                isSaveToFile = true;
                                logger.LogInformation("Will be save an image file when capture the screens");
                                break;
                            }

                        case "threshold":
                            {
                                double.TryParse(config_value, out double t);
                                threshold = t;
                                break;
                            }

                        case "sample_clipboard":
                            {
                                sampleClipboard = new List<string>(config_value.Split(':'));
                                break;
                            }

                        case "sample_ocr":
                            {
                                tesseractDataPath = Path.Combine(workingDirectory, "app/assets/tessdata_best");
                                tesseractLanguage = "eng";
                                sampleOcr = new List<string>(config_value.Split(':'));
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

                        case "sample_nodup_width":
                            {
                                int.TryParse(config_value, out int w);
                                sampleNodupSize.Width = w;
                                break;
                            }

                        case "sample_nodup_height":
                            {
                                int.TryParse(config_value, out int h);
                                sampleNodupSize.Height = h;
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
            logger.LogInformation($"Failed to read the directory structure: {ex.Message}");
        }

        foreach (var file in files)
        {
            string filename = Path.GetFileName(file);

            string realpath;
            string altpath = parent.GetUserVariablesHandler().GetValue(filename);
            if (!String.IsNullOrEmpty(altpath))
            {
                realpath = altpath;
                logger.LogInformation($"Use the alternative image: {realpath}");
            }
            else
            {
                realpath = file;
                logger.LogInformation($"Use the default image: {realpath}");
            }

            Bitmap bitmap = new Bitmap(realpath)
            {
                Tag = filename
            };

            if (!filename.StartsWith("no_"))
            {
                if (filename.StartsWith("binary_"))
                {
                    templateImages.Add(ImageQuantize(bitmap));
                }
                else
                {
                    templateImages.Add(bitmap);
                }
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
            SetBusy(true);

            switch (mode)
            {
                case "screen":    // 화면 기준
                    results = CaptureAndMatchAllScreens();
                    SetBusy(false);
                    break;

                case "window":    // 윈도우 핸들 기준
                    results = CaptureAndMatchAllWindows();
                    SetBusy(false);
                    break;

                default:
                    SetBusy(false);
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

            Bitmap templateImage = templateImages[templateCurrentIndex];
            string templateName = templateImage.Tag as string;
            TemplateInfo nextTemplateInfo = parent.GetNextTemplateInfo();

            Size templateSize = new Size
            {
                Width = templateImage.Width,
                Height = templateImage.Height
            };

            logger.LogInformation($"Trying match the template {templateName} on the screen {i}...");

            if (!String.IsNullOrEmpty(nextTemplateInfo.FileName) && templateName != nextTemplateInfo.FileName)
            {
                logger.LogInformation($"Ignored the template {templateName}");
                break;
            }

            Bitmap out_mainImage;
            string out_filename;
            if (templateName.StartsWith("binary_"))
            {
                out_mainImage = ImageQuantize((Bitmap)mainImage.Clone());
                out_filename = $"{DateTime.Now:yyyy-MM-dd hh mm ss} binary.png";
            }
            else
            {
                out_mainImage = mainImage;
                out_filename = $"{DateTime.Now:yyyy-MM-dd hh mm ss}.png";
            }

            if (isSaveToFile)
            {
                string out_filepath = Path.Combine(outputDirectoryPath, out_filename);
                ((Bitmap)out_mainImage.Clone()).Save(out_filepath);
                logger.LogInformation($"Screenshot saved: {out_filepath}");
            }

            // List to store the positions of matched templates in the main image
            List<Point> matchPositions;

            // If the index value is negative, retrieve and use an outdated image from the queue
            if (nextTemplateInfo.Index < 0)
            {
                logger.LogInformation($"Finding a previous screen of {nextTemplateInfo.FileName}...");

                Bitmap outdatedImage = null;
                try
                {
                    // Since outdatedSamples is also used to detect duplicate work, we do not delete tasks with Dequeue.
                    foreach (var image in outdatedSamples)
                    {
                        if (image.Tag != null &&
                            ((SampleInfo)image.Tag).FileName == nextTemplateInfo.FileName)
                        {
                            outdatedImage = image;
                            logger.LogInformation($"Found the previous screen of {nextTemplateInfo.FileName}");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogInformation($"Error finding a previous screen: {ex.Message}");
                }

                // Find the matching positions of the outdated image in the main image
                if (outdatedImage != null) {
                    matchPositions = FindTemplate(out_mainImage, outdatedImage);
                    if (matchPositions.Count > 0)
                    {
                        logger.LogInformation("Match found with the outdated image");
                    }
                    else
                    {
                        logger.LogInformation("No match found with the outdated image");
                    }
                }
                else
                {
                    logger.LogInformation("No match found an outdated image");
                    matchPositions = new List<Point>();
                }
            }
            else
            {
                // If the index is not negative, use the current image for template matching
                matchPositions = FindTemplate(out_mainImage, (Bitmap)templateImage.Clone());
            }

            foreach (Point matchPosition in matchPositions)
            {
                try
                {
                    string text = sampleAny.Contains(templateName) ?
                        InspectSample((Bitmap)mainImage.Clone(), matchPosition, templateSize, templateName, sampleSize) : string.Empty;

                    results.Add(new ScreenMatchResult
                    {
                        FileName = templateName,
                        ScreenNumber = i,
                        Position = matchPosition,
                        Text = text
                    });

                    break;  // Only one
                }
                catch (Exception ex)
                {
                    logger.LogInformation($"Ignore the match. {ex.Message}");
                }
            }
        }

        if (results.Count > 0)
        {
            logger.LogInformation("Match found");
        }
        else
        {
            logger.LogInformation($"No match found");
        }

        templateCurrentIndex = ++templateCurrentIndex % templateImages.Count;

        return results;
    }

    public Bitmap CropBitmap(Bitmap bitmap, Point matchPosition, Size templateSize, Size sampleSize, int dx = 0, int dy = 0)
    {
        // Adjust coordinates to the center
        int x = matchPosition.X + (templateSize.Width / 2);
        int y = matchPosition.Y + (templateSize.Height / 2);

        // Set range of crop image
        int cropX = Math.Max((x - sampleSize.Width / 2) + dx, 0);
        int cropY = Math.Max((y - sampleSize.Height / 2) + dy, 0);
        int cropWidth = Math.Min(sampleSize.Width, bitmap.Width - cropX);
        int cropHeight = Math.Min(sampleSize.Height, bitmap.Height - cropY);
        Rectangle cropArea = new Rectangle(cropX, cropY, cropWidth, cropHeight);

        // Crop image
        return bitmap.Clone(cropArea, bitmap.PixelFormat);
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

        // initialize the text
        string text = "";

        // Crop image
        Bitmap croppedBitmap = CropBitmap(bitmap, matchPosition, templateSize, sampleSize, sampleAdjustX, sampleAdjustY);

        // Save to the outdated samples
        if (sampleNodup.Contains(templateName))
        {
            Bitmap croppedNodupBitmap = CropBitmap(bitmap, matchPosition, templateSize, sampleNodupSize);
            uint bitmapCrc32 = ComputeBitmapCrc32(croppedNodupBitmap);
            croppedNodupBitmap.Tag = new SampleInfo(templateName, bitmapCrc32);

            bool bitmapExists = outdatedSamples.Any(x => ((SampleInfo)x.Tag).Crc32 == bitmapCrc32);
            if (bitmapExists)
            {
                throw new InvalidOperationException($"This may be a duplicate request. {templateName}");
            }
            else
            {
                outdatedSamples.Enqueue(croppedNodupBitmap);
                logger.LogInformation($"Added to the image queue. {templateName}");
            }
        }

        // if use Clipboard
        if (sampleClipboard.Contains(templateName))
        {
            logger.LogInformation($"Trying to use the clipboard... {templateName}");
            Thread th = new Thread(new ThreadStart(() =>
            {
                try
                {
                    Clipboard.SetImage((Bitmap)croppedBitmap.Clone());
                    logger.LogInformation($"Copied the image to Clipboard");
                }
                catch (Exception ex)
                {
                    logger.LogInformation($"Failed to copy to the clipboard: {ex.Message}");
                }
            }));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        // if use OCR
        if (sampleOcr.Contains(templateName))
        {
            try
            {
                using (var engine = new TesseractEngine(tesseractDataPath, tesseractLanguage, EngineMode.Default))
                {
                    using (var page = engine.Process(croppedBitmap))
                    {
                        text = page.GetText();

                        logger.LogInformation($"Mean confidence: {page.GetMeanConfidence()}");
                        logger.LogInformation($"Text (GetText): {text}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogInformation($"Failed to OCR: {ex.Message}");
            }
        }

        return text;
    }

    public Bitmap CaptureScreen(Screen screen)
    {
        Rectangle screenSize = screen.Bounds;
        decimal scalingFactor = GetScreenScalingFactor(screen);

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
                        string templateName = image.Tag as string;
                        Size templateSize = new Size
                        {
                            Width = image.Width,
                            Height = image.Height
                        };

                        List<Point> matchPositions = FindTemplate(windowImage, image);
                        matchPositions.ForEach((matchPosition) =>
                        {
                            try
                            {
                                string text = sampleAny.Contains(templateName) ?
                                    InspectSample((Bitmap)windowImage.Clone(), matchPosition, templateSize, templateName, sampleSize) : string.Empty;

                                results.Add(new ScreenMatchResult
                                {
                                    FileName = templateName,
                                    WindowHandle = hWnd,
                                    WindowTitle = windowTitle,
                                    ProcessName = processName,
                                    WindowPosition = windowPosition,
                                    Position = matchPosition,
                                    Text = text
                                });
                            }
                            catch (Exception ex)
                            {
                                logger.LogInformation($"Ignore the match. {ex.Message}");
                            }
                        });
                    }
                }
                catch (Exception ex) {
                    logger.LogInformation($"Error {ex.Message}");
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

    private Bitmap ImageQuantize(Bitmap image, int levels = 4)
    {
        Bitmap quantizedImage = new Bitmap(image.Width, image.Height);
        if (image.Tag != null)
        {
            quantizedImage.Tag = image.Tag;
        }

        int step = 255 / (levels - 1);  // step by step..... ooh baby...(?)

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                // Convert the pixel to grayscale
                Color pixelColor = image.GetPixel(x, y);
                byte grayValue = (byte)((pixelColor.R + pixelColor.G + pixelColor.B) / 3);

                // Convert the grayscale value to the quantize value
                byte quantizedValue = (byte)((grayValue / step) * step);

                // Renew the colors
                Color quantizedColor = Color.FromArgb(quantizedValue, quantizedValue, quantizedValue);
                quantizedImage.SetPixel(x, y, quantizedColor);
            }
        }

        return quantizedImage;
    }

    private uint ComputeBitmapCrc32(Bitmap bitmap)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

            byte[] bitmapBytes = ms.ToArray();
            Crc32 crc32 = new Crc32();
            crc32.Append(bitmapBytes);

            return BitConverter.ToUInt32(crc32.GetCurrentHash(), 0);
        }
    }
}
