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
                ServiceMain svc = new ServiceMain();
                svc.TestStartupAndStop(args);
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new ServiceMain()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
