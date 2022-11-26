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
            modLogging.LogEvent("XRFAgent is running in debug mode", EventLogEntryType.Warning);
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
            OnStop();
        }

        protected override void OnStart(string[] args)
        {
            Stopwatch LoadTime = new Stopwatch();
            LoadTime.Start();

            modLogging.Load();
            modLogging.LogEvent("XRFAgent starting", EventLogEntryType.Information);
            modUpdate.CheckVersion();
            modDatabase.Load();
            modLogging.LogEvent("Database connected", EventLogEntryType.Information);
            modNetwork.Load();
            modSync.Load();

            LoadTime.Stop();
            modLogging.LogEvent("XRFAgent started in " + LoadTime.Elapsed.Milliseconds + " ms", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            modLogging.LogEvent("XRFAgent stopping", EventLogEntryType.Information);
            modSync.Unload();
            modNetwork.Unload();
            modDatabase.Unload();
            modLogging.LogEvent("XRFAgent stopped", EventLogEntryType.Information);
        }
    }
}
