using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace InvisibleGorillaXRay.Linux.Managers
{
    public static class LinuxPipeManager
    {
        private static readonly string SocketPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".invisiblegorilla-xray-linux.sock");

        public static Action<string> OnReceiveArg = delegate { };

        public static void ListenForPipes()
        {
            Task.Run(() =>
            {
                try
                {
                    if (File.Exists(SocketPath)) File.Delete(SocketPath);
                    var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                    listener.Bind(new UnixDomainSocketEndPoint(SocketPath));
                    listener.Listen(5);
                    while (true)
                    {
                        var client = listener.Accept();
                        using var stream = new NetworkStream(client);
                        using var reader = new StreamReader(stream);
                        string message = reader.ReadToEnd();
                        Dispatcher.UIThread.InvokeAsync(() => OnReceiveArg.Invoke(message));
                        client.Close();
                    }
                }
                catch { }
            });
        }

        public static void SignalOpenedApp(string[] args)
        {
            try
            {
                using var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                client.Connect(new UnixDomainSocketEndPoint(SocketPath));
                using var stream = new NetworkStream(client);
                using var writer = new StreamWriter(stream);
                writer.WriteLine(args[0]);
                writer.Flush();
            }
            catch { }
        }

        public static void SignalThisApp(string[] args)
        {
            OnReceiveArg.Invoke(args[0]);
        }
    }
}
