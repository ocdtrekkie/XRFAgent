using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace XRFAgent
{
    internal class modCommand
    {
        /// <summary>
        /// Loading the command module is NOT NEEDED
        /// </summary>
        public static void Load() { }

        /// <summary>
        /// Unloading the command module is NOT NEEDED
        /// </summary>
        public static void Unload() { }

        public static void Handle(string inputCommand, string inputSource, string requestAuth)
        {
            string outputResponse = null;
            int result = 0;
            modLogging.LogEvent("Command, source: " + inputSource + ", authority: " + requestAuth + ", command: " + inputCommand, EventLogEntryType.Information);

            string[] inputData = inputCommand.Split(' ');
            switch (inputData[0])
            {
                case "hac":
                case "hacontroller":
                    string inputCommandTrimmed = inputCommand.Remove(0, inputData[0].Length + 1);
                    result = modDatabase.EnqueueLocalMessage(new modDatabase.LocalQueue { Src = inputSource, Auth = requestAuth, Dest = "hac", Mesg = inputCommandTrimmed, Recv = false });
                    if (result == 1) { outputResponse = "Message to HAController queued"; } else { outputResponse = "Message to HAController not queued"; }
                    break;
                case "reboot" when inputData.Length == 2:
                case "restart" when inputData.Length == 2:
                    if (inputData[1] == "host") { outputResponse = modSystem.RebootHost(); } break;
                case "shutdown" when inputData.Length == 2:
                    if (inputData[1] == "host") { outputResponse = modSystem.ShutdownHost(); } break;
                case "update" when inputData.Length == 2:
                    switch (inputData[1])
                    {
                        case "agent":
                            result = modUpdate.UpdateAgent();
                            switch (result)
                            {
                                case -1:
                                    outputResponse = "Update error"; break;
                                case 0:
                                    outputResponse = "No update needed"; break;
                                default:
                                    outputResponse = "Updating"; break;
                            }
                            break;
                        default: break;
                    }
                    break;
                default: break;
            }
            if (outputResponse == null)
            {
                outputResponse = "Unable to process command";
            }
            modLogging.LogEvent("Response, command: " + inputCommand + ", response: " + outputResponse, EventLogEntryType.Information);
        }
    }
}
