using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;

namespace XRFAgent
{
    internal class modNetwork
    {
        private static System.Timers.Timer PingTimer;
        private static string Ping_InternetCheckAddress;
        private static string Ping_PublicIPSource;
        public static bool IsOnline = true; // assume until tested

        /// <summary>
        /// Loads the network module: Schedules connectivity checks and checks the public IP
        /// </summary>
        public static void Load()
        {
            Ping_InternetCheckAddress = modDatabase.GetConfig("Ping_InternetCheckAddress");
            if (string.IsNullOrEmpty(Ping_InternetCheckAddress))
            {
                modDatabase.AddConfig(new modDatabase.Config { Key = "Ping_InternetCheckAddress", Value = "4.2.2.2" });
                Ping_InternetCheckAddress = "4.2.2.2";
            }

            Ping_PublicIPSource = modDatabase.GetConfig("Ping_PublicIPSource");
            if (string.IsNullOrEmpty(Ping_PublicIPSource))
            {
                modDatabase.AddConfig(new modDatabase.Config { Key = "Ping_PublicIPSource", Value = "dyndns" });
                Ping_PublicIPSource = "dyndns";
            }

            PingTimer = new System.Timers.Timer();
            PingTimer.Elapsed += new ElapsedEventHandler(PingInternetHandler);
            PingTimer.Interval = 60000; // 1 min
            PingTimer.Enabled = true;
            modLogging.LogEvent("Connectivity checks scheduled", EventLogEntryType.Information);

            Thread InitialGetIP = new Thread(InitialGetIPHandler);
            InitialGetIP.Start();
        }

        /// <summary>
        /// Unloads the network module: Stops connectivity checks
        /// </summary>
        public static void Unload()
        {
            if (PingTimer != null)
            {
                PingTimer.Stop();
            }
        }

        /// <summary>
        /// Gets this agent's local network address
        /// </summary>
        /// <returns>(string) Local IP address</returns>
        public static string GetLocalIPAddress()
        {
            if (IsOnline == true)
            {
                // https://stackoverflow.com/a/44226831
                // TODO: We need to account for wireless adapters too.
                List<string> iplist = NetworkInterface.GetAllNetworkInterfaces()
                   .Where(x => x.NetworkInterfaceType == NetworkInterfaceType.Ethernet && x.OperationalStatus == OperationalStatus.Up)
                   .SelectMany(x => x.GetIPProperties().UnicastAddresses)
                   .Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork)
                   .Select(x => x.Address.ToString())
                   .ToList();

                foreach (string ip in iplist)
                {
                    string newLocalIP = ip;

                    string oldLocalIP = modDatabase.GetConfig("Ping_LastKnownLocalIP");
                    if (oldLocalIP != newLocalIP)
                    {
                        modLogging.LogEvent("Local IP address changed from " + oldLocalIP + " to " + newLocalIP, EventLogEntryType.Warning, 6014);
                        modDatabase.AddOrUpdateConfig(new modDatabase.Config { Key = "Ping_LastKnownLocalIP", Value = newLocalIP });
                    }

                    return newLocalIP;
                }

                return "No valid local IP address found";
            }
            else return "Not online";
        }

        /// <summary>
        /// Checks a public service for this agent's public IP address
        /// </summary>
        /// <returns>(string) Public IP address</returns>
        public static string GetPublicIPAddress()
        {
            if (IsOnline == true)
            {
                try
                {
                    if (Ping_PublicIPSource == "dyndns")
                    {
                        string reqUrl = "http://checkip.dyndns.org";
                        WebRequest req = WebRequest.Create(reqUrl);
                        WebResponse resp = req.GetResponse();
                        StreamReader sr = new StreamReader(resp.GetResponseStream());
                        string response = sr.ReadToEnd().Trim();
                        string[] responseArray = response.Split(':');
                        string[] responseArray2 = responseArray[1].Split('<');
                        string newPublicIP = responseArray2[0].Trim();

                        string oldPublicIP = modDatabase.GetConfig("Ping_LastKnownPublicIP");
                        if (oldPublicIP != newPublicIP)
                        {
                            modLogging.LogEvent("Public IP address changed from " + oldPublicIP + " to " + newPublicIP, EventLogEntryType.Warning, 6013);
                            modDatabase.AddOrUpdateConfig(new modDatabase.Config { Key = "Ping_LastKnownPublicIP", Value = newPublicIP });
                        }

                        return newPublicIP;
                    }
                    else return "No valid public IP source set";
                }
                catch (Exception err)
                {
                    modLogging.LogEvent(err.Message, EventLogEntryType.Error, 6012);
                    return "Error";
                }
            }
            else return "Not online";
        }

        /// <summary>
        /// Attempts a specified number of pings until it receives a successful response
        /// </summary>
        /// <param name="Host">(string) Destination</param>
        /// <param name="repeat">(int) Number of times to try</param>
        /// <returns>(string) Result of ping attempt</returns>
        public static string SendPing(string Host, int repeat = 1)
        {
            try
            {
                Ping a = new Ping();
                PingReply b;
                string responseData = "";
                PingOptions c = new PingOptions();
                c.DontFragment = true;
                c.Ttl = 64;
                string data = "aaaaaaaaaaaaaaaa";
                Byte[] bt = Encoding.ASCII.GetBytes(data);
                Int16 i;
                for (i = 1; i < repeat; i++)
                {
                    b = a.Send(Host, 2000, bt, c);
                    if (b.Status == IPStatus.Success)
                    {
                        responseData = "Reply from " + Host + " in " + b.RoundtripTime.ToString() + " ms, ttl " + b.Options.Ttl;
                        return responseData;
                    }
                    else
                    {
                        responseData = b.Status.ToString();
                    }
                }
                return responseData;
            }
            catch (Exception err)
            {
                modLogging.LogEvent(err.Message, EventLogEntryType.Error, 6011);
                return "Ping error";
            }
        }

        /// <summary>
        /// Handler to launch initial public IP check on a new Thread
        /// </summary>
        private static void InitialGetIPHandler()
        {
            GetLocalIPAddress();
            GetPublicIPAddress();
        }

        /// <summary>
        /// Handler to launch scheduled connectivity checks
        /// </summary>
        /// <param name="sender">(object) Sender</param>
        /// <param name="e">(EventArgs) Event Arguments</param>
        private static void PingInternetHandler(object sender, EventArgs e)
        {
            string response = "";
            response = SendPing(Ping_InternetCheckAddress, 4);
            if (response.StartsWith("Reply from"))
            {
                if (IsOnline == false)
                {
                    modLogging.LogEvent("System is now connected to the Internet", EventLogEntryType.Information);
                    GetPublicIPAddress();
                }
                IsOnline = true;
            }
            else
            {
                if (IsOnline == true)
                {
                    modLogging.LogEvent("System is not connected to the Internet", EventLogEntryType.Warning);
                }
                IsOnline = false;
            }
        }
    }
}
