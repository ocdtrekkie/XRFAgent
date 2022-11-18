using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace XRFAgent
{
    public partial class XRFAgent : ServiceBase
    {
        public XRFAgent()
        {
            InitializeComponent();
        }

        public void OnDebug(string[] args)
        {
            OnStart(args);
            modLogging.Log_Event("XRFAgent is running in debug mode", EventLogEntryType.Warning);
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
            OnStop();
        }

        protected override void OnStart(string[] args)
        {
            Stopwatch LoadTime = new Stopwatch();
            LoadTime.Start();

            modLogging.Load();
            modLogging.Log_Event("XRFAgent starting", EventLogEntryType.Information);
            modUpdate.Check_Version();
            modDatabase.Load();
            modLogging.Log_Event("Database connected", EventLogEntryType.Information);
            modNetwork.Load();
            modSync.Load();
            modIpcServer.Load();

            LoadTime.Stop();
            modLogging.Log_Event("XRFAgent started in " + LoadTime.Elapsed.Milliseconds + " ms", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            modLogging.Log_Event("XRFAgent stopping", EventLogEntryType.Information);
            modIpcServer.Unload();
            modSync.Unload();
            modNetwork.Unload();
            modDatabase.Unload();
            modLogging.Log_Event("XRFAgent stopped", EventLogEntryType.Information);
        }
    }
}
