using Microsoft.Extensions.Logging;
using System.IO;
using System;

namespace WelsonJS.Service.Logging
{
    public class FileLogger : ILogger
    {
        private string loggingDirectory;
        private string categoryName;
        private static object _lock = new object();

        public FileLogger(string _loggingDirectory, string _categoryName = "welsonjs")
        {
            loggingDirectory = _loggingDirectory;
            categoryName = _categoryName;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            //return logLevel == LogLevel.Trace;
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter != null)
            {
                lock (_lock)
                {
                    try
                    {
                        if (!Directory.Exists(loggingDirectory))
                        {
                            Directory.CreateDirectory(loggingDirectory);
                        }

                        string path = Path.Combine(loggingDirectory, $"{categoryName}_service.{DateTime.Now.ToString("yyyy-MM-dd")}.log");
                        string nl = Environment.NewLine;
                        string err = "";

                        if (exception != null)
                        {
                            err = nl + exception.GetType() + ": " + exception.Message + nl + exception.StackTrace + nl;
                        }

                        File.AppendAllText(path, logLevel.ToString() + ": [" + DateTime.Now.ToString() + "] " + formatter(state, exception) + nl + err);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(LogLevel.Warning.ToString() + ": [" + DateTime.Now.ToString() + "] Failed to write a log file. " + ex.Message);
                    }
                }
            }
        }
    }
}
