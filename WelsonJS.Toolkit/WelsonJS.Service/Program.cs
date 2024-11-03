using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.ServiceProcess;
using WelsonJS.Service.Logging;

namespace WelsonJS.Service
{
    internal static class Program
    {
        private static ILogger logger;

        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        /// 
        static void Main(string[] args)
        {
            // create the logger
            ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
            factory.AddDirectory(Path.GetTempPath());
            logger = factory.CreateLogger("welsonjs");

            // create the service
            if (Environment.UserInteractive)
            {
                Console.WriteLine("WelsonJS Service Application (User Interactive Mode)");
                Console.WriteLine("https://github.com/gnh1201/welsonjs");
                Console.WriteLine();
                Console.WriteLine("Service is running...");

                ServiceMain svc = new ServiceMain(args, logger);
                svc.TestStartupAndStop();
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new ServiceMain(args, logger)
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
