using System;
using System.Drawing;

namespace WelsonJS.Service
{
    public class ScreenMatchResult
    {
        public string FileName { get; set; }
        public int ScreenNumber { get; set; }
        public IntPtr WindowHandle { get; set; }
        public string WindowTitle { get; set; }
        public Point Location { get; set; }
        public double MaxCorrelation { get; set; }

        public override string ToString()
        {
            return $"Template: {FileName}, Screen Number: {ScreenNumber}, Window Title: {WindowTitle}, " +
                   $"Location: (x: {Location.X}, y: {Location.Y}), " +
                   $"Max Correlation: {MaxCorrelation}";
        }
    }
}
