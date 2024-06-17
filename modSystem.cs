using System;
using System.Diagnostics;
using Microsoft.Win32;

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

        /// <summary>
        /// Gets a full Windows build number from the registry
        /// </summary>
        /// <returns>(string) Build number</returns>
        public static string GetWindowsVersion()
        {
            try
            {
                RegistryKey currentVersion = Registry.LocalMachine.OpenSubKey("SOFTWARE\\MICROSOFT\\Windows NT\\CurrentVersion");
                string currentWindowsVersion = currentVersion.GetValue("CurrentMajorVersionNumber").ToString() + "." + currentVersion.GetValue("CurrentMinorVersionNumber").ToString() + "." + currentVersion.GetValue("CurrentBuild").ToString() + "." + currentVersion.GetValue("UBR").ToString();

                string oldWindowsVersion = modDatabase.GetConfig("System_LastKnownWindowsVersion");
                if (oldWindowsVersion != currentWindowsVersion)
                {
                    modDatabase.AddOrUpdateConfig(new modDatabase.Config { Key = "System_LastKnownWindowsVersion", Value = currentWindowsVersion });
                }

                return currentWindowsVersion;
            }
            catch(Exception err)
            {
                modLogging.LogEvent("Unable to get registry information: " + err.Message, EventLogEntryType.Error, 6032);
                return "Registry error";
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
