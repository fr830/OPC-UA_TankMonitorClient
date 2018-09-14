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
        Timer timer = new Timer();
        OPCClient opcClient;
        ApplicationInstance application = new ApplicationInstance();
        string serverUrl = TankDataTypes.serverUrl;

        public MonitoringService()
        {
            InitializeComponent();

            // create and init OPC Client
            application.ApplicationType = ApplicationType.Client;
            application.ConfigSectionName = "Quickstarts.ReferenceClient";

            // load the application configuration.
            application.LoadApplicationConfiguration(false).Wait();
            // check the application certificate.
             //application.CheckApplicationInstanceCertificate(false, 0).Wait();
            opcClient = new OPCClient(application.ApplicationConfiguration, serverUrl);
        }

        internal void TestStartupAndStop(string[] args)
        {
            this.OnStart(args);
            Console.ReadLine();
            this.OnStop();
        }

        protected override void OnStart(string[] args)
        {
            using (EventLog eventLog = new EventLog("Application"))
            {
                eventLog.Source = "OPC_Service";
                eventLog.WriteEntry("Iniciado serviço", EventLogEntryType.Information, 101, 1);
            }

            WriteToFile("Service is started at " + DateTime.Now);
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 5000; //number in milisecinds  
            timer.Enabled = true;

            try
            {
                // run the opc client
                // opcClient.run();

                WriteToFile("OPC Client is started at " + DateTime.Now);
            }
            catch (Exception e)
            {
                Console.WriteLine(application.ApplicationName, e);
                return;
            }
        }

        protected override void OnStop()
        {
            WriteToFile("Monitoring Service is stopped at " + DateTime.Now);
        }
        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            WriteToFile("Monitoring Service is recall at " + DateTime.Now);
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