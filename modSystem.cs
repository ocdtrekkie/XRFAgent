using System;
using System.Diagnostics;

namespace XRFAgent
{
    internal class modSystem
    {
        /// <summary>
        /// Loading the system module is NOT NEEDED
        /// </summary>
        public static void Load() { }

        /// <summary>
        /// Unloading the system module is NOT NEEDED
        /// </summary>
        public static void Unload() { }

        public static string InstallQuickAssist()
        {
            if (modNetwork.IsOnline == true)
            {
                try
                {
                    Process InstallProcess = new Process();
                    InstallProcess.StartInfo.FileName = "winget";
                    InstallProcess.StartInfo.Arguments = "install 9P7BP5VNWKX5 -s msstore --accept-source-agreements --accept-package-agreements";
                    InstallProcess.StartInfo.UseShellExecute = false;
                    InstallProcess.StartInfo.CreateNoWindow = true;
                    InstallProcess.StartInfo.RedirectStandardOutput = true;
                    InstallProcess.StartInfo.RedirectStandardError = true;
                    InstallProcess.Start();
                    modLogging.LogEvent("Install result: " + InstallProcess.StandardOutput.ReadToEnd() + "\n" + InstallProcess.StandardError.ReadToEnd(), EventLogEntryType.Information);
                    return "Installation complete";
                }
                catch(Exception err)
                {
                    modLogging.LogEvent("Software installation error: " + err.Message, EventLogEntryType.Error, 6032);
                    return "Installation error";
                }
            }
            else
            {
                return "Unable to install software while offline";
            }
        }

        /// <summary>
        /// Reboots the host computer
        /// </summary>
        /// <returns>(string) Response</returns>
        public static string RebootHost()
        {
            try
            {
                Process.Start("shutdown", "-r");
                return "Reboot started";
            }
            catch(Exception err)
            {
                modLogging.LogEvent("Unable to initiate reboot: " + err.Message, EventLogEntryType.Error, 6031);
                return "Reboot error";
            }
        }

        /// <summary>
        /// Shuts down the host computer
        /// </summary>
        /// <returns>(string) Response</returns>
        public static string ShutdownHost()
        {
            try
            {
                Process.Start("shutdown", "-s");
                return "Shutdown started";
            }
            catch(Exception err)
            {
                modLogging.LogEvent("Unable to initiate shutdown: " + err.Message, EventLogEntryType.Error, 6031);
                return "Shutdown failed";
            }
        }
    }
}
