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
        // TODO Create setting for Verbose logging

        /// <summary>
        /// Loads the logging module: Creates event log object
        /// </summary>
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

        /// <summary>
        /// Unloading the logging module is NOT NEEDED
        /// </summary>
        public static void Unload() { }

        /// <summary>
        /// Writes an event to the Windows Event Log
        /// </summary>
        /// <param name="LogMessage">(string) Contents of the log message</param>
        /// <param name="LogType">(EventLogEntryType) Severity of log message</param>
        /// <param name="LogID">(int) Event ID or error code</param>
        public static void Log_Event(string LogMessage, EventLogEntryType LogType, int LogID = 0)
        {
            agentlog.WriteEntry(LogMessage, LogType, LogID);
        }
    }
}
