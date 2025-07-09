using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Text.Json;
using Microsoft.VisualBasic.Devices;
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

        public static void AttachEventWatcher()
        {
            EventLogQuery logQuery = new EventLogQuery("System", PathType.LogName, "*[System[Provider[@Name='disk'] and (EventID=7 or EventID=11 or EventID=25 or EventID=26 or EventID=51 or EventID=55)]]");
            EventLogWatcher logWatcher = new EventLogWatcher(logQuery);
            logWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(EventWritten);
            logWatcher.Enabled = true;
        }

        private static void EventWritten(Object obj, EventRecordWrittenEventArgs arg)
        {
            modLogging.LogEvent("Detected disk issue", EventLogEntryType.Error, 6061);
            modSync.SendSingleConfig("Alert_DiskFailure", "reported");
        }

        /// <summary>
        /// Collects the list of installed applications, updates the local table, and sends to the server.
        /// </summary>
        /// <returns>Result of new applications detected</returns>
        public static string GetInstalledSoftware()
        {
            try
            {
                List<RegistryKey> UninstallKeys = new List<RegistryKey>() { Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"), Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall") };
                RegistryKey SoftwareKey;
                string SoftwareKeyName;
                string NewSoftware = ""; string NewSoftwareJSON = "{\"newsoftware\":[";
                int count = 0;
                int result = 0;
                modDatabase.InstalledSoftware SoftwareObj;
                foreach (RegistryKey UninstallKey in UninstallKeys)
                {
                    foreach (string ProgramKey in UninstallKey.GetSubKeyNames())
                    {
                        SoftwareKey = UninstallKey.OpenSubKey(ProgramKey);
                        if (SoftwareKey != null)
                        {
                            SoftwareKeyName = SoftwareKey.GetValue("DisplayName")?.ToString() ?? "";
                            if (SoftwareKeyName != "")
                            {
                                SoftwareObj = new modDatabase.InstalledSoftware { Name = SoftwareKeyName, Version = SoftwareKey.GetValue("DisplayVersion")?.ToString() ?? "", Publisher = SoftwareKey.GetValue("Publisher")?.ToString() ?? "", InstallDate = SoftwareKey.GetValue("InstallDate")?.ToString() ?? "" };
                                result = modDatabase.UpdateSoftware(SoftwareObj);
                                if (result == 0)
                                {
                                    NewSoftware = NewSoftware + SoftwareKeyName + ", ";
                                    NewSoftwareJSON = NewSoftwareJSON + JsonSerializer.Serialize(SoftwareObj) + ",";
                                    result = modDatabase.AddSoftware(SoftwareObj);
                                }
                                count++;
                            }
                        }
                    }
                }
                if (NewSoftware != "")
                {
                    NewSoftware = NewSoftware.Substring(0, NewSoftware.Length - 2);
                    NewSoftwareJSON = NewSoftwareJSON.Substring(0, NewSoftwareJSON.Length - 1) + "]}";
                    modLogging.LogEvent("Detected new software installed: " + NewSoftware, EventLogEntryType.Information, 6051);
                    modSync.SendMessage("server", "nodedata", "newsoftware", NewSoftwareJSON);
                }
                return "Installed Applications: " + count.ToString();
            }
            catch (Exception err)
            {
                modLogging.LogEvent("Unable to get registry information: " + err.Message + "\n\n" + err.StackTrace, EventLogEntryType.Error, 6032);
                return "Registry error";
            }
        }

        /// <summary>
        /// Drops the installed software inventory, then gathers an updated version.
        /// </summary>
        /// <returns>Result of new applications detected</returns>
        public static string ResetInstalledSoftware()
        {
            modLogging.LogEvent("Reset installed software inventory.", EventLogEntryType.Information, 6052);
            modDatabase.TruncateSoftware();
            return GetInstalledSoftware();
        }

        /// <summary>
        /// Collects some general system information
        /// </summary>
        /// <returns>(string) Result</returns>
        public static string GetSystemDetails()
        {
            try
            {
                string SystemDetailsJSON = "{\"systemdetails\":[";
                modDatabase.Config ConfigObj;

                ConfigObj = new modDatabase.Config { Key = "System_Hostname", Value = Environment.MachineName };
                modDatabase.AddOrUpdateConfig(ConfigObj);
                SystemDetailsJSON = SystemDetailsJSON + JsonSerializer.Serialize(ConfigObj) + ",";

                RegistryKey currentVersion = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\MICROSOFT\Windows NT\CurrentVersion");

                ConfigObj = new modDatabase.Config { Key = "System_LastKnownWindowsVersion", Value = currentVersion.GetValue("CurrentMajorVersionNumber").ToString() + "." + currentVersion.GetValue("CurrentMinorVersionNumber").ToString() + "." + currentVersion.GetValue("CurrentBuild").ToString() + "." + currentVersion.GetValue("UBR").ToString() };
                modDatabase.AddOrUpdateConfig(ConfigObj);
                SystemDetailsJSON = SystemDetailsJSON + JsonSerializer.Serialize(ConfigObj) + ",";

                ConfigObj = new modDatabase.Config { Key = "System_OSProductName", Value = currentVersion.GetValue("ProductName").ToString() };
                modDatabase.AddOrUpdateConfig(ConfigObj);
                SystemDetailsJSON = SystemDetailsJSON + JsonSerializer.Serialize(ConfigObj) + ",";

                RegistryKey systemHardware = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");

                ConfigObj = new modDatabase.Config { Key = "System_BaseBoardManufacturer", Value = systemHardware.GetValue("BaseBoardManufacturer").ToString() };
                modDatabase.AddOrUpdateConfig(ConfigObj);
                SystemDetailsJSON = SystemDetailsJSON + JsonSerializer.Serialize(ConfigObj) + ",";

                ConfigObj = new modDatabase.Config { Key = "System_BaseBoardProduct", Value = systemHardware.GetValue("BaseBoardProduct").ToString() };
                modDatabase.AddOrUpdateConfig(ConfigObj);
                SystemDetailsJSON = SystemDetailsJSON + JsonSerializer.Serialize(ConfigObj) + ",";

                ConfigObj = new modDatabase.Config { Key = "System_SystemManufacturer", Value = systemHardware.GetValue("SystemManufacturer").ToString() };
                modDatabase.AddOrUpdateConfig(ConfigObj);
                SystemDetailsJSON = SystemDetailsJSON + JsonSerializer.Serialize(ConfigObj) + ",";

                ConfigObj = new modDatabase.Config { Key = "System_SystemProductName", Value = systemHardware.GetValue("SystemProductName").ToString() };
                modDatabase.AddOrUpdateConfig(ConfigObj);
                SystemDetailsJSON = SystemDetailsJSON + JsonSerializer.Serialize(ConfigObj) + ",";

                RegistryKey systemCPU = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                ConfigObj = new modDatabase.Config { Key = "System_ProcessorName", Value = systemCPU.GetValue("ProcessorNameString").ToString() };
                modDatabase.AddOrUpdateConfig(ConfigObj);
                SystemDetailsJSON = SystemDetailsJSON + JsonSerializer.Serialize(ConfigObj) + ",";

                ComputerInfo VBCI = new ComputerInfo();
                ConfigObj = new modDatabase.Config { Key = "System_TotalPhysicalMemory", Value = VBCI.TotalPhysicalMemory.ToString() };
                modDatabase.AddOrUpdateConfig(ConfigObj);
                SystemDetailsJSON = SystemDetailsJSON + JsonSerializer.Serialize(ConfigObj) + ",";

                DriveInfo[] allDrives = DriveInfo.GetDrives();
                string dLetter;
                foreach (DriveInfo d in allDrives)
                {
                    dLetter = d.Name.Substring(0, 1);
                    ConfigObj = new modDatabase.Config { Key = "System_Drive_" + dLetter + "_Type", Value = d.DriveType.ToString() };
                    modDatabase.AddOrUpdateConfig(ConfigObj);
                    SystemDetailsJSON = SystemDetailsJSON + JsonSerializer.Serialize(ConfigObj) + ",";
                    if (d.IsReady == true && d.DriveType == DriveType.Fixed)
                    {
                        ConfigObj = new modDatabase.Config { Key = "System_Drive_" + dLetter + "_Label", Value = d.VolumeLabel };
                        modDatabase.AddOrUpdateConfig(ConfigObj);
                        SystemDetailsJSON = SystemDetailsJSON + JsonSerializer.Serialize(ConfigObj) + ",";

                        ConfigObj = new modDatabase.Config { Key = "System_Drive_" + dLetter + "_TotalSize", Value = d.TotalSize.ToString() };
                        modDatabase.AddOrUpdateConfig(ConfigObj);
                        SystemDetailsJSON = SystemDetailsJSON + JsonSerializer.Serialize(ConfigObj) + ",";

                        ConfigObj = new modDatabase.Config { Key = "System_Drive_" + dLetter + "_TotalFreeSpace", Value = d.TotalFreeSpace.ToString() };
                        modDatabase.AddOrUpdateConfig(ConfigObj);
                        SystemDetailsJSON = SystemDetailsJSON + JsonSerializer.Serialize(ConfigObj) + ",";
                    }
                }

                if (File.Exists(@"C:\HAC\HAController.exe"))
                {
                    ConfigObj = new modDatabase.Config { Key = "HAController_Version", Value = FileVersionInfo.GetVersionInfo(@"C:\HAC\HAController.exe").FileVersion };
                    modDatabase.AddOrUpdateConfig(ConfigObj);
                    SystemDetailsJSON = SystemDetailsJSON + JsonSerializer.Serialize(ConfigObj) + ",";
                }

                SystemDetailsJSON = SystemDetailsJSON.Substring(0, SystemDetailsJSON.Length - 1) + "]}";
                modSync.SendMessage("server", "nodedata", "systemdetails", SystemDetailsJSON);

                return "System details updated";
            }
            catch (Exception err)
            {
                modLogging.LogEvent("Unable to get registry information: " + err.Message + "\n\n" + err.StackTrace, EventLogEntryType.Error, 6032);
                return "Registry error";
            }
        }

        /// <summary>
        /// Installs updates for Windows, installs WindowsUpdatePush tool if it is not present
        /// </summary>
        /// <returns>(int) Return code</returns>
        public static int InstallWindowsUpdates()
        {
            try
            {
                if (File.Exists(Properties.Settings.Default.Tools_FolderURI + "WindowsUpdatePush.exe") == false)
                {
                    int installResult = modUpdate.InstallWindowsUpdatePush();
                    if (installResult == -1)
                    {
                        return -1;
                    }
                }
                Process UpdateRunner = new Process();
                UpdateRunner.StartInfo.UseShellExecute = false;
                UpdateRunner.StartInfo.RedirectStandardOutput = true;
                UpdateRunner.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                UpdateRunner.StartInfo.FileName = Properties.Settings.Default.Tools_FolderURI + "WindowsUpdatePush.exe";
                UpdateRunner.Start();
                UpdateRunner.WaitForExit();
                modLogging.LogEvent(UpdateRunner.StandardOutput.ReadToEnd(), EventLogEntryType.Information, 6042);
                return UpdateRunner.ExitCode;
            }
            catch(Exception err)
            {
                modLogging.LogEvent("Windows Update error: " + err.Message, EventLogEntryType.Error, 6041);
                return -1;
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

        /// <summary>
        /// Configures the system Run dialog, which is frequently used by phone scammers
        /// </summary>
        /// <param name="action">(string) "enable" or "disable"</param>
        /// <returns>(string) Response</returns>
        public static string ConfigureRunDialog(string action)
        {
            int newvalue = 0;
            if (action == "disable") { newvalue = 1; }
            RegistryKey explorerPolicies = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", true);
            explorerPolicies.SetValue("NoRun", newvalue, RegistryValueKind.DWord);
            explorerPolicies.Close();

            modSync.SendSingleConfig("Security_RunDialog", action + "d");
            return "Run dialog " + action + "d";
        }

        /// <summary>
        /// Disables installing or using any browser extensions on Chrome, Edge, and Firefox
        /// but permits uBlock Origin, Privacy Badger, and Facebook Container on Firefox
        /// </summary>
        /// <returns></returns>

        public static string DisableWebExtensions()
        {
            RegistryKey chromeExtensions = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Google\Chrome\ExtensionInstallBlocklist", true);
            chromeExtensions.SetValue("1", "*", RegistryValueKind.String);
            RegistryKey edgeExtensions = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Edge\ExtensionInstallBlocklist", true);
            edgeExtensions.SetValue("1", "*", RegistryValueKind.String);
            RegistryKey firefoxExtensions = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Mozilla\Firefox", true);
            string[] firefoxExtensionPolicy = {"{", "  \"*\":{", "    \"blocked_install_message\": \"Unapproved extensions are not permitted.\",", "    \"install_sources\": [\"about:addons\",\"https://addons.mozilla.org/\"],", "    \"installation_mode\": \"blocked\",", "    \"allowed_types\": [\"extension\"]", "  },", "  \"uBlock0@raymondhill.net\":{", "    \"installation_mode\": \"allowed\"", "  },", "  \"jid1-MnnxcxisBPnSXQ@jetpack\":{", "    \"installation_mode\": \"allowed\"", "  },", "  \"@contain-facebook\":{", "    \"installation_mode\": \"allowed\"", "  }", "}" };
            firefoxExtensions.SetValue("ExtensionSettings", firefoxExtensionPolicy, RegistryValueKind.MultiString);

            modSync.SendSingleConfig("Security_WebExtensions", "disabled");
            return "Browser extensions disabled";
        }

        /// <summary>
        /// Disables the browser Notifications API on Chrome, Edge, and Firefox
        /// </summary>
        /// <returns>(string) Response</returns>
        public static string DisableWebNotifications()
        {
            RegistryKey chromePolicies = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Google\Chrome", true);
            chromePolicies.SetValue("DefaultNotificationsSetting", 2, RegistryValueKind.DWord);
            RegistryKey edgePolicies = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Edge", true);
            edgePolicies.SetValue("DefaultNotificationsSetting", 2, RegistryValueKind.DWord);
            RegistryKey firefoxPolicies = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Mozilla\Firefox\Permissions\Notifications", true);
            firefoxPolicies.SetValue("BlockNewRequests", 1, RegistryValueKind.DWord);

            modSync.SendSingleConfig("Security_WebNotifications", "disabled");
            return "Browser notifications disabled";
        }
    }
}
