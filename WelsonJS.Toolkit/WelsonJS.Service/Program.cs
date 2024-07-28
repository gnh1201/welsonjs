using System;
using System.ServiceProcess;

namespace WelsonJS.Service
{
    internal static class Program
    {
        /// <summary>
        /// entry point
        /// </summary>
        static void Main(string[] args)
        {
            ServiceMain svc = new ServiceMain();

            if (Environment.UserInteractive)
            {
                Console.WriteLine("Welcome to WelsonJS Scripting Service...");
                Console.WriteLine("https://github.com/gnh1201/welsonjs");
                svc.TestStartupAndStop(args);
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    svc
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
