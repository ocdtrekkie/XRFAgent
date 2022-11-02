using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XRFAgent
{
    internal class modLogging
    {
        public static EventLog agentlog;

        public static void Load()
        {
            if (EventLog.SourceExists("XRFAgent") == false)
            {
                EventLog.CreateEventSource("XRFAgent", "Application");
            }
            agentlog = new EventLog();
            agentlog.Source = "XRFAgent";
            agentlog.Log = "Application";
        }

        // public static void Unload() NOT NEEDED

        public static void Log_Event(string LogMessage, EventLogEntryType LogType, int LogID = 0)
        {
            agentlog.WriteEntry(LogMessage, LogType, LogID);
        }
    }
}
