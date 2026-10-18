using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace InvisibleManXRay.Services.UI
{
    using Models;
    using Values;

    /// <summary>
    /// Реализация <see cref="IConnectionService"/> поверх legacy-конвейера MainWindow
    /// (Func/Action-колбэки в WindowFactory → Core/Handlers).
    ///
    /// Инкапсулирует BackgroundWorker-модель: блокирующие вызовы ядра
    /// (LoadConfig / EnableMode / Run) выполняются вне UI-потока.
    /// Наследует <see cref="Service"/>, чтобы регистрироваться в ServicesManager.
    /// </summary>
    public class ConnectionService : Service, IConnectionService
    {
        // Legacy-делегаты, прокидываются из WindowFactory при создании MainWindow.
        private Func<Status> loadConfig;
        private Func<Status> enableMode;
        private Action<string> runServer;
        private Action stopServer;
        private Action cancelServer;
        private Action disableMode;

        private BackgroundWorker worker;
        private volatile bool isBusy;
        private volatile bool isRerunRequested;

        public bool IsBusy => isBusy;

        public void Setup(
            Func<Status> loadConfig,
            Func<Status> enableMode,
            Action<string> runServer,
            Action stopServer,
            Action cancelServer,
            Action disableMode)
        {
            this.loadConfig = loadConfig;
            this.enableMode = enableMode;
            this.runServer = runServer;
            this.stopServer = stopServer;
            this.cancelServer = cancelServer;
            this.disableMode = disableMode;

            InitializeWorker();
        }

        private void InitializeWorker()
        {
            worker = new BackgroundWorker();

            // Rerun: смена конфига/режима во время активного соединения.
            worker.RunWorkerCompleted += (sender, e) =>
            {
                if (!isRerunRequested)
                    return;

                isRerunRequested = false;
                worker.RunWorkerAsync();
            };

            worker.DoWork += (sender, e) =>
            {
                // 1. Загрузка конфигурации.
                Status configStatus = loadConfig.Invoke();
                if (configStatus.Code == Code.ERROR)
                {
                    e.Result = configStatus;
                    return;
                }

                // 2. Включение режима (System Proxy / TUN).
                Status modeStatus = enableMode.Invoke();
                if (modeStatus.Code == Code.ERROR)
                {
                    e.Result = modeStatus;
                    return;
                }

                // Отменено пользователем на фазе «Подключение».
                if (modeStatus.Code == Code.INFO && modeStatus.SubCode == SubCode.CANCELED)
                {
                    e.Result = modeStatus;
                    return;
                }

                // 3. Соединение активно — блокируем поток до Stop/Cancel/Rerun.
                runServer.Invoke(configStatus.Content.ToString());

                e.Result = modeStatus;
            };
        }

        /// <inheritdoc/>
        public Task<Status> ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (worker.IsBusy || isBusy)
                return Task.FromResult(new Status(Code.INFO, SubCode.SUCCESS, null));

            isBusy = true;
            worker.RunWorkerAsync();

            return WaitCompletionAsync(cancellationToken);

            async Task<Status> WaitCompletionAsync(CancellationToken token)
            {
                var tcs = new TaskCompletionSource<Status>();

                void OnCompleted(object sender, RunWorkerCompletedEventArgs args)
                {
                    isBusy = false;
                    Status result = args.Result as Status ?? new Status(Code.SUCCESS, SubCode.SUCCESS, null);
                    tcs.TrySetResult(result);
                }

                worker.RunWorkerCompleted += OnCompleted;

                using (token.Register(() => cancelServer?.Invoke()))
                {
                    Status result = await tcs.Task.ConfigureAwait(false);
                    worker.RunWorkerCompleted -= OnCompleted;
                    return result;
                }
            }
        }

        /// <inheritdoc/>
        public void Cancel()
        {
            if (!isBusy)
                return;

            cancelServer?.Invoke();
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (!isBusy)
                return;

            stopServer?.Invoke();
        }

        /// <inheritdoc/>
        public void StopWithDisabledMode()
        {
            if (!isBusy)
                return;

            stopServer?.Invoke();
            disableMode?.Invoke();
        }

        /// <inheritdoc/>
        public void Restart()
        {
            if (!isBusy)
                return;

            isRerunRequested = true;
            stopServer?.Invoke();

        /// <inheritdoc/>
        public void RestartWithDisabledMode()
        {
            if (!isBusy)
                return;

            isRerunRequested = true;
            disableMode?.Invoke();
            stopServer?.Invoke();
    }
}
