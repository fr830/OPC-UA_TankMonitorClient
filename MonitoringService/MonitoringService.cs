using Opc.Ua;
using Opc.Ua.Configuration;
using Quickstarts.ReferenceClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MonitoringService
{
    public partial class MonitoringService : ServiceBase
    {
        OPCClient opcClient;
        ApplicationInstance application;
        string serverUrl;

        int isFisrtTime=0;
        System.Timers.Timer timer = new System.Timers.Timer();
        

        public MonitoringService()
        {
            InitializeComponent();

            application = new ApplicationInstance();
            application.ApplicationType = ApplicationType.Client;
            application.ConfigSectionName = "Quickstarts.ReferenceClient";

            //// load the application configuration.
            string configPath = AppDomain.CurrentDomain.BaseDirectory + @"\Quickstarts.ReferenceClient.Config.xml";
            application.LoadApplicationConfiguration(configPath, true);

            //// check the application certificate.
            application.CheckApplicationInstanceCertificate(false, 0);

            // set OPC server endpoint
            serverUrl = TankDataTypes.serverUrl;

            opcClient = new OPCClient(application.ApplicationConfiguration, serverUrl);
        }

        internal void TestStartupAndStop(string[] args)
        {
            this.OnStart(args);

            Console.ReadLine();
            //this.OnStop();
        }

        protected override void OnStart(string[] args)
        {

            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = "OPC_Service";
                eventLog.WriteEntry("Iniciado serviço", EventLogEntryType.Information, 101, 1);
            }

            //run the opc client
            opcClient.Run();

            WriteToFile("Service is started at " + DateTime.Now);

            //timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            //timer.Interval = 15000; //number in milisecinds  
            //timer.Enabled = true;
        }

        //private void OnElapsedTime(object source, ElapsedEventArgs e)
        //{
        //    WriteToFile("Monitoring Service  - OnElapsedTime ! " + DateTime.Now);
        //    if (isFisrtTime == -1)
        //    {
        //        timer.Interval = 1555000;
        //        try
        //        {



        //            WriteToFile("OPC Client is started at " + DateTime.Now);
        //        }
        //        catch (Exception ex)
        //        {
        //            WriteToFile(application.ApplicationName + ex);
        //            return;
        //        }

        //        string sExecutablePath = "C:\\Project\\Cliente_sem WinService\\SampleApplications\\bin\\Debug\\Quickstarts.ReferenceClient.exe";
        //        myProcess = System.Diagnostics.Process.Start(sExecutablePath);
        //        isFisrtTime = +1;
        //        myProcess.WaitForExit();
        //    }
        //}

        protected override void OnStop()
        {
            opcClient.Stop();
            WriteToFile("Monitoring Service is stopped at " + DateTime.Now);
        }

        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}