﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace MonitoringService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            //MonitoringService ms = new MonitoringService();
            //ms.TestStartupAndStop(null);

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new MonitoringService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
