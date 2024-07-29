using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;

namespace WelsonJS.Service
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine("WelsonJS Service Application (User Interactive Mode)");
                Console.WriteLine("https://github.com/gnh1201/welsonjs");
                Console.WriteLine();
                Console.WriteLine("Service is running...");

                ServiceMain svc = new ServiceMain(args);
                svc.TestStartupAndStop();
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new ServiceMain(args)
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
