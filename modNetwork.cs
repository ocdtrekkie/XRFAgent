using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
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

        public static void Load()
        {
            Ping_InternetCheckAddress = modDatabase.GetConfig("Ping_InternetCheckAddress");
            if (string.IsNullOrEmpty(Ping_InternetCheckAddress))
            {
                modDatabase.AddConfig(new modDatabase.Config { Key = "Ping_InternetCheckAddress", Value = "4.2.2.2" });
            }

            Ping_PublicIPSource = modDatabase.GetConfig("Ping_PublicIPSource");
            if (string.IsNullOrEmpty(Ping_PublicIPSource))
            {
                modDatabase.AddConfig(new modDatabase.Config { Key = "Ping_PublicIPSource", Value = "dyndns" });
            }

            PingTimer = new System.Timers.Timer();
            PingTimer.Elapsed += new ElapsedEventHandler(PingInternet);
            PingTimer.Interval = 60000; // 1 min
            PingTimer.Enabled = true;
            modLogging.Log_Event("Connectivity checks scheduled", EventLogEntryType.Information);

            Thread InitialGetPublicIP = new Thread(InitialGetPublicIPHandler);
            InitialGetPublicIP.Start();
        }

        public static void Unload()
        {
            if (PingTimer != null)
            {
                PingTimer.Stop();
            }
        }

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
                            modLogging.Log_Event("Public IP address changed from " + oldPublicIP + " to " + newPublicIP, EventLogEntryType.Warning);
                            modDatabase.AddOrUpdateConfig(new modDatabase.Config { Key = "Ping_LastKnownPublicIP", Value = newPublicIP });
                        }

                        return newPublicIP;
                    }
                    else return "No valid public IP source set";
                }
                catch (Exception err)
                {
                    modLogging.Log_Event(err.Message, EventLogEntryType.Error, 6012);
                    return "Error";
                }
            }
            else return "Not online";
        }

        public static void PingInternet(object sender, EventArgs e)
        {
            string response = "";
            response = SendPing(Ping_InternetCheckAddress);
            if (response.StartsWith("Reply from"))
            {
                if (IsOnline == false)
                {
                    modLogging.Log_Event("System is now connected to the Internet", EventLogEntryType.Information);
                    GetPublicIPAddress();
                }    
                IsOnline = true;
            }
            else
            {
                if (IsOnline == true)
                {
                    modLogging.Log_Event("System is not connected to the Internet", EventLogEntryType.Warning);
                }
                IsOnline = false;
            }
        }

        public static string SendPing(string Host, int repeat = 1)
        {
            try
            {
                Ping a = new Ping();
                PingReply b;
                string txtlog = "";
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
                        txtlog += "Reply from " + Host + " in " + b.RoundtripTime.ToString() + " ms, ttl " + b.Options.Ttl;
                    }
                    else
                    {

                        txtlog += b.Status.ToString();
                    }
                    if (i != repeat)
                    {
                        txtlog += Environment.NewLine;
                    }
                }
                return txtlog;
            }
            catch (Exception err)
            {
                modLogging.Log_Event(err.Message, EventLogEntryType.Error, 6011);
                return "Ping error";
            }
        }

        public static void InitialGetPublicIPHandler()
        {
            GetPublicIPAddress();
        }
    }
}
