using System;
using System.IO;
using System.IO.Pipes;
using System.Windows;
using System.Threading.Tasks;

namespace InvisibleGorillaXRay.Managers
{
    public static class PipeManager
    {
        private const string PIPE_NAME = "InvisibleGorillaXRayPipe";

        public static Action<string> OnReceiveArg = delegate{};

        public static void ListenForPipes()
        {
            Task.Run(() => {
                while(true)
                {
                    using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PIPE_NAME))
                    {
                        pipeServer.WaitForConnection();

                        using (StreamReader reader = new StreamReader(pipeServer))
                        {
                            string message = reader.ReadToEnd();
                            Application.Current?.Dispatcher?.BeginInvoke(new Action(delegate {
                                OnReceiveArg.Invoke(message);
                            }));
                        }
                    }
                }
            });
        }

        public static void SignalOpenedApp(string[] args)
        {
            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PIPE_NAME))
            {
                pipeClient.Connect();

                using (StreamWriter writer = new StreamWriter(pipeClient))
                {
                    writer.WriteLine(args[0]);
                    writer.Flush();
                }
            }
        }

        public static void SignalThisApp(string[] args)
        {
            OnReceiveArg.Invoke(args[0]);
        }
    }
}
