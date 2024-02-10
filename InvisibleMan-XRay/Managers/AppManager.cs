using System;
using System.Threading;

namespace InvisibleManXRay.Managers
{
    using Foundation;
    using Values;

    public class AppManager
    {
        private static Mutex mutex;
        private const string APP_GUID = "{7I6N0VI4-S9I1-43bl-A0eM-72A47N6EDH8M}";

        public void Initialize()
        {
            AvoidRunningMultipleInstances(
                onCreatedNew: () => {
                    SetApplicationCurrentDirectory();
                },
                onAlreadyRunning: () => {
                    ExitFromApplication();
                }
            );
        }

        private void AvoidRunningMultipleInstances(
            Action onCreatedNew,
            Action onAlreadyRunning
        )
        {
            mutex = new Mutex(true, APP_GUID, out bool isCreatedNew);
            
            if(isCreatedNew)
                onCreatedNew.Invoke();
            else
                MessageBox.Show(
                    message: Message.APP_ALREADY_RUNNING,
                    onResult: (result) => onAlreadyRunning.Invoke()
                );
        }

        private void SetApplicationCurrentDirectory()
        {
            Environment.CurrentDirectory = System.IO.Path.GetDirectoryName(
                path: Environment.ProcessPath
            );
        }

        private void ExitFromApplication() => Environment.Exit(0);
    }
}