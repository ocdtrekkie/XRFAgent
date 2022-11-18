using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XRFAgent
{
    internal class modIpcServer
    {
        private static NamedPipeServer listen;

        /// <summary>
        /// Loads the IPC server module
        /// </summary>
        public static void Load()
        {
            listen = new NamedPipeServer();
            listen.Start();
            listen.Received += Listen_Received;
        }

        /// <summary>
        /// Unloads the IPC server module
        /// </summary>
        public static void Unload()
        {
            listen.Stop();
        }

        private static void Listen_Received(object sender, DataReceivedEventArgs e)
        {
            modLogging.Log_Event(e.Data, EventLogEntryType.Warning);
        }

        public sealed class NamedPipeServer : IIpcServer
        {
            private NamedPipeServerStream server;

            private void OnReceived(DataReceivedEventArgs e)
            {
                var handler = this.Received;

                if (handler != null)
                {
                    handler(this, e);
                }
            }

            public event EventHandler<DataReceivedEventArgs> Received;

            public void Start()
            {
                server = new NamedPipeServerStream("XRFAgentCommandServer", PipeDirection.In);

                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        try
                        {
                            this.server.WaitForConnection();

                            using (var reader = new StreamReader(this.server,Encoding.UTF8,false,1024,true))
                            {
                                this.OnReceived(new DataReceivedEventArgs(reader.ReadToEnd()));
                            }
                        }
                        catch(IOException)
                        {
                            this.server.Disconnect();
                        }
                    }
                });
            }

            public void Stop()
            {
                this.server.Disconnect();
            }

            void IDisposable.Dispose()
            {
                this.Stop();
                this.server.Dispose();
            }
        }

        public interface IIpcClient
        {
            void Send(string data);
        }

        public interface IIpcServer : IDisposable
        {
            void Start();
            void Stop();

            event EventHandler<DataReceivedEventArgs> Received;
        }

        public sealed class DataReceivedEventArgs : EventArgs
        {
            public DataReceivedEventArgs(string data)
            {
                this.Data = data;
            }

            public string Data { get; private set; }
        }
    }
}
