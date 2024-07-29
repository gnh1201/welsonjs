/*
 * WelsonJS.Service 
 * 
 *     filename:
 *         ScreenTimer.cs
 * 
 *     description:
 *         WelsonJS - Build a Windows app on the Windows built-in JavaScript engine
 * 
 *     website:
 *         - https://github.com/gnh1201/welsonjs
 *         - https://catswords.social/@catswords_oss
 *         - https://teams.live.com/l/community/FEACHncAhq8ldnojAI
 * 
 *     author:
 *         Namhyeon Go <abuse@catswords.net>
 *
 *     license:
 *         GPLv3 or MS-RL(Microsoft Reciprocal License)
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.ServiceProcess;
using System.Timers;
using System.Windows.Forms;
using WelsonJS.Service;

public class ScreenTimer : System.Timers.Timer
{
    private static List<Bitmap> templateImages = new List<Bitmap>();
    private string templateDirectory;

    public string WorkingDirectory { get; set; }
    public ServiceBase Parent { get; set; }

    public class MatchedResult
    {
        public string FileName;
        public int ScreenNumber;
        public Point Location;
        public double MaxCorrelation;
    }

    public ScreenTimer()
    {
        Elapsed += OnTimeElapsed;

        templateDirectory = Path.Combine(WorkingDirectory, "assets/img/templates");
        LoadTemplateImages();
    }

    private void OnTimeElapsed(object sender, ElapsedEventArgs e)
    {
        List<MatchedResult> matchedResults = CaptureAndMatchAllScreens();

        ServiceMain svc = (ServiceMain)Parent;
        matchedResults.ForEach(result =>
        {
            svc.Log(svc.DispatchServiceEvent("screenTime", new object[] {
                result.FileName,
                result.ScreenNumber,
                result.Location.X,
                result.Location.Y,
                result.MaxCorrelation
            }));
        });
    }

    public void LoadTemplateImages()
    {
        string[] imageFiles = Directory.GetFiles(templateDirectory, "*.png");
        foreach (string file in imageFiles)
        {
            templateImages.Add(new Bitmap(file));
        }
    }

    public List<MatchedResult> CaptureAndMatchAllScreens()
    {
        var results = new List<MatchedResult>();

        for (int i = 0; i < Screen.AllScreens.Length; i++)
        {
            Screen screen = Screen.AllScreens[i];
            Bitmap mainImage = CaptureScreen(screen);

            foreach (var templateImage in templateImages)
            {
                Point matchLocation = FindTemplate(mainImage, templateImage, out double maxCorrelation);
                string templateFileName = templateImage.Tag as string;

                results.Add(new MatchedResult
                {
                    FileName = templateFileName,
                    ScreenNumber = i,
                    Location = matchLocation,
                    MaxCorrelation = maxCorrelation
                });
            }
        }

        return results;
    }

    public Bitmap CaptureScreen(Screen screen)
    {
        Rectangle screenSize = screen.Bounds;
        Bitmap bitmap = new Bitmap(screenSize.Width, screenSize.Height);

        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(screenSize.Left, screenSize.Top, 0, 0, screenSize.Size);
        }

        return bitmap;
    }

    public Point FindTemplate(Bitmap mainImage, Bitmap templateImage, out double maxCorrelation)
    {
        int mainWidth = mainImage.Width;
        int mainHeight = mainImage.Height;
        int templateWidth = templateImage.Width;
        int templateHeight = templateImage.Height;

        Point bestMatch = Point.Empty;
        maxCorrelation = double.MinValue;

        for (int x = 0; x <= mainWidth - templateWidth; x++)
        {
            for (int y = 0; y <= mainHeight - templateHeight; y++)
            {
                double correlation = CalculateCorrelation(mainImage, templateImage, x, y);
                if (correlation > maxCorrelation)
                {
                    maxCorrelation = correlation;
                    bestMatch = new Point(x, y);
                }
            }
        }

        return bestMatch;
    }

    private double CalculateCorrelation(Bitmap mainImage, Bitmap templateImage, int offsetX, int offsetY)
    {
        int templateWidth = templateImage.Width;
        int templateHeight = templateImage.Height;

        double sumTemplate = 0;
        double sumTemplateSquare = 0;
        double sumMain = 0;
        double sumMainSquare = 0;
        double sumProduct = 0;

        for (int x = 0; x < templateWidth; x++)
        {
            for (int y = 0; y < templateHeight; y++)
            {
                Color mainPixel = mainImage.GetPixel(x + offsetX, y + offsetY);
                Color templatePixel = templateImage.GetPixel(x, y);

                double mainGray = (mainPixel.R + mainPixel.G + mainPixel.B) / 3.0;
                double templateGray = (templatePixel.R + templatePixel.G + templatePixel.B) / 3.0;

                sumTemplate += templateGray;
                sumTemplateSquare += templateGray * templateGray;
                sumMain += mainGray;
                sumMainSquare += mainGray * mainGray;
                sumProduct += mainGray * templateGray;
            }
        }

        int numPixels = templateWidth * templateHeight;
        double numerator = (numPixels * sumProduct) - (sumMain * sumTemplate);
        double denominator = Math.Sqrt(((numPixels * sumMainSquare) - (sumMain * sumMain)) * ((numPixels * sumTemplateSquare) - (sumTemplate * sumTemplate)));

        return denominator == 0 ? 0 : numerator / denominator;
    }
}