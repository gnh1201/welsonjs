using Microsoft.Extensions.Logging;

namespace WelsonJS.Service.Logging
{
    public static class FileLoggerExtensions
    {
        public static ILoggerFactory AddDirectory(this ILoggerFactory factory, string loggingDirectory)
        {
            factory.AddProvider(new FileLoggerProvider(loggingDirectory));
            return factory;
        }
    }
}
