using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
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

        /// <summary>
        /// Loads the sync module: Schedules heartbeats
        /// </summary>
        public static void Load()
        {
            Sync_ServerURL = modDatabase.GetConfig("Sync_ServerURL");
            Sync_SandstormToken = modDatabase.GetConfig("Sync_SandstormToken");
            Sync_AccessKey = modDatabase.GetConfig("Sync_AccessKey");

            if (string.IsNullOrEmpty(Sync_ServerURL) || string.IsNullOrEmpty(Sync_SandstormToken) || string.IsNullOrEmpty(Sync_AccessKey))
            {
                modLogging.LogEvent("Sync settings are not configured!", EventLogEntryType.Error, 6001);
                return;
            }

            HeartbeatTimer = new System.Timers.Timer();
            HeartbeatTimer.Elapsed += new ElapsedEventHandler(SendHeartbeatHandler);
            HeartbeatTimer.Interval = 300000; // 5 mins
            HeartbeatTimer.Enabled = true;

            Thread InitialSyncHeartbeat = new Thread(InitialHeartbeatHandler);
            InitialSyncHeartbeat.Start();
            modLogging.LogEvent("Sync heartbeat scheduled", EventLogEntryType.Information);
        }

        /// <summary>
        /// Unloads the sync module: Stops heartbeats
        /// </summary>
        public static void Unload()
        {
            if (HeartbeatTimer != null)
            {
                HeartbeatTimer.Stop();
            }
        }

        /// <summary>
        /// Sends a message to the sync server
        /// </summary>
        /// <param name="Destination">(string) Intended destination of the message</param>
        /// <param name="MessageType">(string) Type of message being sent</param>
        /// <param name="Message">(string) Contents of message being sent</param>
        /// <param name="ExtendedMessage">(string) Contents of extended message data</param>
        public static async void SendMessage(string Destination, string MessageType, string Message, string ExtendedMessage = "")
        {
            // TODO Only send messages if we are online
            try
            {
                HttpClient MessageClient = new HttpClient();
                string publicIP = modDatabase.GetConfig("Ping_LastKnownPublicIP");
                string windowsVersion = modDatabase.GetConfig("System_LastKnownWindowsVersion");
                HttpRequestMessage MessageBuilder = new HttpRequestMessage(HttpMethod.Post, Sync_ServerURL + "?message_type=" + MessageType + "&destination=" + Destination + "&access_key=" + Sync_AccessKey + "&message=" + Message + "&user_agent=XRFAgent/" + Assembly.GetExecutingAssembly().GetName().Version + "&ip_address=" + publicIP + "&windows_version=" + windowsVersion);
                TimeSpan httpTimeout = TimeSpan.FromSeconds(10);
                MessageClient.Timeout = httpTimeout;
                string EncodedCreds = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes("sandstorm:" + Sync_SandstormToken));
                MessageBuilder.Headers.Add("Authorization", "Basic " + EncodedCreds);
                if (ExtendedMessage != "") { MessageBuilder.Content = new StringContent(ExtendedMessage); }
                HttpResponseMessage MessageResponse = await MessageClient.SendAsync(MessageBuilder);

                if (MessageResponse.IsSuccessStatusCode != true)
                {
                    modLogging.LogEvent("Sync error:" + (int)MessageResponse.StatusCode + " " + MessageResponse.StatusCode, EventLogEntryType.Error, 6002);
                }
                else
                {
                    string ResponseContent = await MessageResponse.Content.ReadAsStringAsync();
                    // VERBOSE: Uncomment to show JSON response from server
                    // modLogging.LogEvent((int)MessageResponse.StatusCode + " " + MessageResponse.StatusCode + " " + ResponseContent, EventLogEntryType.Information);

                    if (ResponseContent != "[]")
                    {
                        using (JsonDocument messagesReceived = JsonDocument.Parse(ResponseContent))
                        {
                            foreach (JsonElement element in messagesReceived.RootElement.EnumerateArray())
                            {
                                string source = element.GetProperty("source").ToString();
                                string mesg = element.GetProperty("mesg").ToString();
                                modCommand.Handle(mesg, "sync", source);
                            }
                        }
                    }
                }
                MessageResponse.Dispose();
            }
            catch (Exception err)
            {
                modLogging.LogEvent(err.Message + "\n\n" + err.StackTrace, EventLogEntryType.Error, 6002);
            }
        }

        /// <summary>
        /// Stores and sends to the server a single configuration value
        /// </summary>
        /// <param name="configkey">Key</param>
        /// <param name="configvalue">Value</param>
        /// <returns>(int) 0</returns>
        public static int SendSingleConfig(string configkey, string configvalue)
        {
            modDatabase.Config ConfigObj = new modDatabase.Config { Key = configkey, Value = configvalue };
            modDatabase.AddOrUpdateConfig(ConfigObj);
            string SystemDetailsJSON = "{\"systemdetails\":[" + JsonSerializer.Serialize(ConfigObj) + "]}";
            modSync.SendMessage("server", "nodedata", "systemdetails", SystemDetailsJSON);
            return 0;
        }

        /// <summary>
        /// Handler to launch initial heartbeat on a new Thread
        /// </summary>
        private static void InitialHeartbeatHandler()
        {
            SendMessage("server", "fetch", "none");
        }

        /// <summary>
        /// Handler to launch scheduled heartbeats
        /// </summary>
        /// <param name="sender">(object) Sender</param>
        /// <param name="e">(EventArgs) Event Arguments</param>
        private static void SendHeartbeatHandler(object sender, EventArgs e)
        {
            SendMessage("server", "fetch", "none");
        }
    }
}
