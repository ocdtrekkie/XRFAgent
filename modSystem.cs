using System;
using System.Collections.Generic;
using System.Diagnostics;
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
