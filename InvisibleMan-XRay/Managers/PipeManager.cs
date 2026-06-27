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
        private const string SHOW_WINDOW_COMMAND = "--show-window";

        public static Action<string> OnReceiveArg = delegate{};
        public static Action OnShowWindow = delegate{};

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
                        if (message.Trim() == SHOW_WINDOW_COMMAND)
                            OnShowWindow.Invoke();
                        else
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

        public static void SignalShowWindow()
        {
            NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PIPE_NAME);
            pipeClient.Connect();

            StreamWriter writer = new StreamWriter(pipeClient);
            writer.WriteLine(SHOW_WINDOW_COMMAND);
            writer.Flush();
            writer.Close();
        }

        public static void SignalThisApp(string[] args)
        {
            OnReceiveArg.Invoke(args[0]);
        }
    }
}