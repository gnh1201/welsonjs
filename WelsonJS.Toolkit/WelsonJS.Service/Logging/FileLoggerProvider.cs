using Microsoft.Extensions.Logging;

namespace WelsonJS.Service.Logging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private string loggingDirectory;

        public FileLoggerProvider(string _loggingDirectory)
        {
            loggingDirectory = _loggingDirectory;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new FileLogger(loggingDirectory, categoryName);
        }

        public void Dispose()
        {
            // Dispose
        }
    }
}
