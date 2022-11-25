using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;

namespace XRFAgent
{
    class modUpdate
    {
        public static WebClient UpdateDownloadClient;

        /// <summary>
        /// Loading the update module is NOT NEEDED
        /// </summary>
        public static void Load() { }

        /// <summary>
        /// Unloading the update module is NOT NEEDED
        /// </summary>
        public static void Unload() { }

        /// <summary>
        /// Checks to see if a newer version of the agent is available
        /// </summary>
        /// <returns>(int) -1 for error, 0 for up-to-date, x for latest version</returns>
        public static int Check_Version()
        {
            if (modNetwork.IsOnline == false)
            {
                modLogging.Log_Event("Update check failed", EventLogEntryType.Error, 6021);
                return -1;
            }    
            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            UpdateDownloadClient = new WebClient();
            int latestVersion = -1;
            try
            {
                latestVersion = int.Parse(UpdateDownloadClient.DownloadString(Properties.Settings.Default.Update_SourceURI + "currentagent.txt"));
            }
            catch(Exception err)
            {
                modLogging.Log_Event(err.Message, EventLogEntryType.Error, 6021);
                return -1;
            }
            if (currentVersion.Revision >= latestVersion)
            {
                modLogging.Log_Event("No update needed", EventLogEntryType.Information);
                return 0;
            }
            else if (currentVersion.Revision < latestVersion)
            {
                modLogging.Log_Event("Application revision is " + currentVersion.Revision.ToString() + ", current version is " + latestVersion.ToString(), EventLogEntryType.Information);
                return latestVersion;
            }
            else
            {
                modLogging.Log_Event("Update check failed", EventLogEntryType.Error, 6021);
                return -1;
            }
        }

        /// <summary>
        /// Checks to see if a newer version of the agent is available and initiates an update
        /// </summary>
        /// <returns>(int) -1 for error, 0 for up-to-date, x for latest version</returns>
        public static int Update_Agent()
        {
            int updateNeeded = Check_Version();
            if (updateNeeded >= 1)
            {
                try
                {
                    try
                    {
                        Directory.Delete(Properties.Settings.Default.Scripts_FolderURI + "agenttemp", true);
                    }
                    catch(DirectoryNotFoundException) { }
                    Directory.CreateDirectory(Properties.Settings.Default.Scripts_FolderURI + "agenttemp");
                    string UpdateFile = "agent" + updateNeeded.ToString() + ".zip";
                    UpdateDownloadClient.DownloadFile(Properties.Settings.Default.Update_SourceURI + UpdateFile, Properties.Settings.Default.Scripts_FolderURI + UpdateFile);
                    ZipFile.ExtractToDirectory(Properties.Settings.Default.Scripts_FolderURI + UpdateFile, Properties.Settings.Default.Scripts_FolderURI + "agenttemp");
                    File.Copy(Properties.Settings.Default.Scripts_FolderURI + @"agenttemp\agentupdate.bat", Properties.Settings.Default.Scripts_FolderURI + "agentupdate.bat", true);
                    Process UpdateRunner = new Process();
                    UpdateRunner.StartInfo.UseShellExecute = true;
                    UpdateRunner.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    UpdateRunner.StartInfo.FileName = Properties.Settings.Default.Scripts_FolderURI + "agentupdate.bat";
                    UpdateRunner.Start();
                }
                catch(Exception err)
                {
                    modLogging.Log_Event(err.Message, EventLogEntryType.Error, 6022);
                    return -1;
                }
            }
            return updateNeeded;
        }
    }
}
