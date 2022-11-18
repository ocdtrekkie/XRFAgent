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
        public static void Load()
        {
            NamedPipeServer listen = new NamedPipeServer();
            listen.Start();
            listen.Received += Listen_Received;
        }

        private static void Listen_Received(object sender, DataReceivedEventArgs e)
        {
            modLogging.Log_Event(e.Data, EventLogEntryType.Warning);
        }

        public sealed class NamedPipeServer : IIpcServer
        {
            private readonly NamedPipeServerStream server = new NamedPipeServerStream("XRFAgentCommandServer", PipeDirection.In);

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
                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        this.server.WaitForConnection();

                        using (var reader = new StreamReader(this.server))
                        {
                            this.OnReceived(new DataReceivedEventArgs(reader.ReadToEnd()));
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
