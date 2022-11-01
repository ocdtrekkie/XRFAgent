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

        protected override void OnStart(string[] args)
        {
            Stopwatch LoadTime = new Stopwatch();
            LoadTime.Start();

            modLogging.Create_Log();
            modLogging.Log_Event("XRFAgent starting", EventLogEntryType.Information);
            modUpdate.Check_Version();
            modDatabase.Connect_DB();
            modLogging.Log_Event("Database connected", EventLogEntryType.Information);

            LoadTime.Stop();
            modLogging.Log_Event("XRFAgent started in " + LoadTime.Elapsed.Milliseconds + " ms", EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            modLogging.Log_Event("XRFAgent stopping", EventLogEntryType.Information);
            modDatabase.Close_DB();
            modLogging.Log_Event("XRFAgent stopped", EventLogEntryType.Information);
        }
    }
}
