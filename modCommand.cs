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
            modLogging.LogEvent("Command, source: " + inputSource + ", authority: " + requestAuth + ", command: " + inputCommand, EventLogEntryType.Information);

            string[] inputData = inputCommand.Split(' ');
            switch (inputData[0])
            {
                case "update" when inputData.Length == 2:
                    switch (inputData[1])
                    {
                        case "agent":
                            int result = modUpdate.UpdateAgent();
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
