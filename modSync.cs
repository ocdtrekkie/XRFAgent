using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Timers;

namespace XRFAgent
{
    internal class modSync
    {
        private static System.Timers.Timer HeartbeatTimer;
        private static string Sync_ServerURL;
        private static string Sync_SandstormToken;
        private static string Sync_AccessKey;

        public static void Load()
        {
            Sync_ServerURL = modDatabase.GetConfig("Sync_ServerURL");
            Sync_SandstormToken = modDatabase.GetConfig("Sync_SandstormToken");
            Sync_AccessKey = modDatabase.GetConfig("Sync_AccessKey");

            if (string.IsNullOrEmpty(Sync_ServerURL) || string.IsNullOrEmpty(Sync_SandstormToken) || string.IsNullOrEmpty(Sync_AccessKey))
            {
                modLogging.Log_Event("Sync settings are not configured!", EventLogEntryType.Error, 6001);
                return;
            }

            HeartbeatTimer = new System.Timers.Timer();
            HeartbeatTimer.Elapsed += new ElapsedEventHandler(SendHeartbeatHandler);
            HeartbeatTimer.Interval = 300000; // 5 mins
            HeartbeatTimer.Enabled = true;

            Thread InitialSyncHeartbeat = new Thread(InitialHeartbeatHandler);
            InitialSyncHeartbeat.Start();
            modLogging.Log_Event("Sync heartbeat scheduled", EventLogEntryType.Information);
        }

        public static void Unload()
        {
            if (HeartbeatTimer != null)
            {
                HeartbeatTimer.Stop();
            }
        }

        public static async void SendMessage(string Destination, string MessageType, string Message)
        {
            // TODO Only send messages if we are online
            try
            {
                HttpClient MessageClient = new HttpClient();
                string publicIP = modDatabase.GetConfig("Ping_LastKnownPublicIP");
                HttpRequestMessage MessageBuilder = new HttpRequestMessage(HttpMethod.Post, Sync_ServerURL + "?message_type=" + MessageType + "&destination=" + Destination + "&access_key=" + Sync_AccessKey + "&message=" + Message + "&user_agent=XRFAgent/" + Assembly.GetExecutingAssembly().GetName().Version + "&ip_address=" + publicIP);
                TimeSpan httpTimeout = TimeSpan.FromSeconds(10);
                MessageClient.Timeout = httpTimeout;
                string EncodedCreds = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes("sandstorm:" + Sync_SandstormToken));
                MessageBuilder.Headers.Add("Authorization", "Basic " + EncodedCreds);
                HttpResponseMessage MessageResponse = await MessageClient.SendAsync(MessageBuilder);

                if (MessageResponse.IsSuccessStatusCode != true)
                {
                    modLogging.Log_Event("Sync error", EventLogEntryType.Error, 6002);
                }
                // TODO Actually get responses and handle them from v1 API
            }
            catch (Exception err)
            {
                modLogging.Log_Event(err.Message, EventLogEntryType.Error, 6002);
            }
        }

        public static void InitialHeartbeatHandler()
        {
            SendMessage("server", "heartbeat", "none");
        }

        public static void SendHeartbeatHandler(object sender, EventArgs e)
        {
            SendMessage("server", "heartbeat", "none");
        }
    }
}
