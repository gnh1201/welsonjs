using System;

namespace WelsonJS.Service.Model
{
    public class FileMatchResult
    {
        public string Id { get; set; }
        public string FilePath { get; set; }
        public ulong Offset { get; set; }
        public string RuleName { get; set; }
        public DateTime LastChecked { get; set; }
    }
}
