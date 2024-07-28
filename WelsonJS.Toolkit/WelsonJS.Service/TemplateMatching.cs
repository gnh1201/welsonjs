// this is prototype code
// https://github.com/gnh1201/welsonjs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using WelsonJS.Service;

public class TemplateMatching
{
    public void Test(string workingDirectory)
    {
        string templateFolderPath = Path.Combine(workingDirectory, "assets/img/templates");

        // Load all template images
        List<Bitmap> templateImages = LoadTemplateImages(templateFolderPath);

        // try template matching
        List<(string FileName, int ScreenNumber, Point Location, double MaxCorrelation)> results =
            CaptureAndMatchAllScreens(templateImages);

        // print results
        foreach (var result in results)
        {
            Console.WriteLine($"Template: {result.FileName}, Screen: {result.ScreenNumber}, " +
                              $"Location: (x: {result.Location.X}, y: {result.Location.Y}), " +
                              $"Max Correlation: {result.MaxCorrelation}");
        }
    }

    public static List<Bitmap> LoadTemplateImages(string folderPath)
    {
        var templates = new List<Bitmap>();
        var files = Directory.GetFiles(folderPath, "*.png");

        foreach (var file in files)
        {
            templates.Add(new Bitmap(file));
        }

        return templates;
    }

    public static List<(string FileName, int ScreenNumber, Point Location, double MaxCorrelation)>
        CaptureAndMatchAllScreens(List<Bitmap> templateImages)
    {
        var results = new List<(string FileName, int ScreenNumber, Point Location, double MaxCorrelation)>();

        for (int i = 0; i < Screen.AllScreens.Length; i++)
        {
            Screen screen = Screen.AllScreens[i];
            Bitmap mainImage = CaptureScreen(screen);

            foreach (var templateImage in templateImages)
            {
                Point matchLocation = FindTemplate(mainImage, templateImage, out double maxCorrelation);
                string templateFileName = templateImage.Tag as string;

                results.Add((templateFileName, i, matchLocation, maxCorrelation));
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

    public static Point FindTemplate(Bitmap mainImage, Bitmap templateImage, out double maxCorrelation)
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

    private static double CalculateCorrelation(Bitmap mainImage, Bitmap templateImage, int offsetX, int offsetY)
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