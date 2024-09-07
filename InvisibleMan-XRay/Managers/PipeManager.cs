using System;
using System.IO;
using System.IO.Pipes;
using System.Windows;
using System.Threading.Tasks;

namespace InvisibleManXRay.Managers
{
    public static class PipeManager
    {
        private const string PIPE_NAME = "InvisibleManXRayPipe";

        public static Action<string> OnReceiveArg = delegate{};

        public static void ListenForPipes()
        {
            Task.Run(() => {
                while(true)
                {
                    NamedPipeServerStream pipeServer = new NamedPipeServerStream(PIPE_NAME);
                    pipeServer.WaitForConnection();

                    StreamReader reader = new StreamReader(pipeServer);
                    string message = reader.ReadToEnd();
                    Application.Current.Dispatcher.BeginInvoke(new Action(delegate {
                        OnReceiveArg.Invoke(message);
                    }));
                    
                    pipeServer.Close();
                    pipeServer.Dispose();
                }
            });
        }

        public static void SignalOpenedApp(string[] args)
        {
            NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PIPE_NAME);
            pipeClient.Connect();

            StreamWriter writer = new StreamWriter(pipeClient);
            writer.WriteLine(args[0]);
            writer.Flush();
            writer.Close();
        }

        public static void SignalThisApp(string[] args)
        {
            OnReceiveArg.Invoke(args[0]);
        }
    }
}