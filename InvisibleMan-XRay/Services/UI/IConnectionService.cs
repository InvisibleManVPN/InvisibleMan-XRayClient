using System.Threading;
using System.Threading.Tasks;

namespace InvisibleManXRay.Services.UI
{
    using Models;

    /// <summary>
    /// Абстракция над XRay-ядром для MVVM-слоя (Шаг 4).
    /// Реализация <see cref="ConnectionService"/> инкапсулирует legacy-конвейер
    /// MainWindow (BackgroundWorker) и предоставляет асинхронный API без
    /// блокировки UI-потока.
    /// </summary>
    public interface IConnectionService
    {
        /// <summary>True, если сейчас идёт подключение или соединение активно.</summary>
        bool IsBusy { get; }

        /// <summary>
        /// Полный цикл подключения: загрузка конфига → включение режима →
        /// удержание соединения до отмены/остановки. Бег в Task — UI не блокируется.
        /// </summary>
        Task<Status> ConnectAsync(CancellationToken cancellationToken = default);

        /// <summary>Отмена фазы «Подключение» (pending).</summary>
        void Cancel();

        /// <summary>Остановить соединение (без выключения режима — rerun-сценарий).</summary>
        void Stop();

        /// <summary>Остановить соединение и выключить режим (полный STOP пользователем).</summary>
        void StopWithDisabledMode();

        /// <summary>
        /// Остановить и перезапустить (rerun) — при смене конфига, пока worker активен.
        /// </summary>
        void Restart();

        /// <summary>
        /// Выключить режим, остановить и перезапустить — при смене настроек режима.
        /// </summary>
        void RestartWithDisabledMode();
    }
}
