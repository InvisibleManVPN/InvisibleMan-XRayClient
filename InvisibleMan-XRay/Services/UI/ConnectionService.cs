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
        // volatile — флаг пишется с UI-потока (Restart), читается на UI в completion,
        // но запись может происходить до завершения DoWork — видимость важна.
        private volatile bool isRerunRequested;

        // Gate для потокобезопасного обмена pending-TCS между ConnectAsync (UI)
        // и RunWorkerCompleted (UI-поток BackgroundWorker) + DoWork не трогает его.
        private readonly object completionGate = new object();
        private TaskCompletionSource<Status> pendingCompletion;

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

            // Постоянный обработчик завершения (разрешает pending TCS из ConnectAsync).
            worker.RunWorkerCompleted += OnWorkerCompleted;

            // Rerun: смена конфига/режима во время активного соединения.
            worker.RunWorkerCompleted += OnRerunCompleted;

            worker.DoWork += (sender, e) =>
            {
                // Прогресс pipeline → состояние Connected сразу после успешного
                // включения режима (Hero-кнопка реагирует мгновенно, до блокирующего Run).
                void OnProgress(object status) => ProgressChanged?.Invoke(this, (Status)status);

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

                ReportRunning(OnProgress);

                // 3. Соединение активно — блокируем поток до Stop/Cancel/Rerun.
                runServer.Invoke(configStatus.Content.ToString());

                e.Result = modeStatus;

                void ReportRunning(Action<object> report)
                {
                    report?.Invoke(new Status(Code.SUCCESS, SubCode.RUNNING, null));
                }
            };
        }

        /// <summary>
        /// Промежуточные статусы pipeline (RUNNING при входе в удержание соединения).
        /// Поднимается с фонового потока — подписчик сам диспатчит в UI.
        /// </summary>
        public event EventHandler<Status> ProgressChanged;

        /// <inheritdoc/>
        public Task<Status> ConnectAsync(CancellationToken cancellationToken = default)
        {
            // Rerun-сценарий: worker уже крутится (удерживает соединение) —
            // новый цикл запускается сам по завершении текущего.
            if (isBusy)
                return Task.FromResult(new Status(Code.INFO, SubCode.SUCCESS, null));

            isBusy = true;
            worker.RunWorkerAsync();

            return WaitCompletionAsync(cancellationToken);

            async Task<Status> WaitCompletionAsync(CancellationToken token)
            {
                var tcs = new TaskCompletionSource<Status>();

                lock (completionGate)
                {
                    pendingCompletion = tcs;
                }

                // Отмена по токену: снимаем pending-TCS первым, чтобы поздний
                // completion worker'а не переписал состояние (cancel-server выполняется в Cancel()).
                using (token.Register(() =>
                {
                    lock (completionGate)
                    {
                        if (pendingCompletion == tcs)
                            pendingCompletion = null;
                    }

                    cancelServer?.Invoke();
                }))
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Rerun: перезапуск worker'а после остановки соединения (смена конфига/режима).
        /// Подписан ПОСЛЕ OnWorkerCompleted — сбрасывает isBusy обратно, т.к. новый цикл уже запущен.
        /// </summary>
        private void OnRerunCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            if (!isRerunRequested)
                return;

            isRerunRequested = false;
            isBusy = true;
            worker.RunWorkerAsync();
        }

        /// <summary>
        /// Единственный постоянный обработчик завершения worker'а (подписан в InitializeWorker).
        /// Разрешает pending TCS; для rerun-циклов без ожидания — no-op.
        /// </summary>
        private void OnWorkerCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            lock (completionGate)
            {
                isBusy = false;

                TaskCompletionSource<Status> tcs = pendingCompletion;
                pendingCompletion = null;

                if (tcs == null)
                    return;

                Status result = args.Result as Status ?? new Status(Code.SUCCESS, SubCode.SUCCESS, null);
                tcs.TrySetResult(result);
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
        public void DisableModeOnly()
        {
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
