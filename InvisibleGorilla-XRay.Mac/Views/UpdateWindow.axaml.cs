using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace InvisibleGorillaXRay.Mac.Views
{
    using Models;
    using Values;
    using InvisibleGorillaXRay.Services;
    using InvisibleGorillaXRay.Services.Analytics.UpdateWindow;

    public partial class UpdateWindow : Window
    {
        private Func<Status> checkForUpdate;
        private Action onUpdateClick;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();
        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        public UpdateWindow()
        {
            InitializeComponent();
            Opened += OnWindowOpened;
        }

        public void Setup(Func<Status> checkForUpdate, Action onUpdateClick)
        {
            this.checkForUpdate = checkForUpdate;
            this.onUpdateClick = onUpdateClick;
        }

        private void OnWindowOpened(object sender, EventArgs e)
        {
            RunUpdateCheck();
        }

        private void RunUpdateCheck()
        {
            ShowCheckForUpdateStatus();

            Task.Run(() =>
            {
                Status updateStatus = checkForUpdate.Invoke();

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    switch (updateStatus.Code)
                    {
                        case Code.ERROR:
                            ShowConnectionErrorStatus();
                            break;
                        case Code.SUCCESS:
                            if (updateStatus.SubCode == SubCode.UPDATE_AVAILABLE)
                                ShowUpdateAvailableStatus();
                            else
                                ShowUpdateUnavailableStatus();
                            break;
                    }
                });
            });
        }

        private void OnUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            onUpdateClick.Invoke();
            AnalyticsService.SendEvent(new UpdateButtonClickedEvent());
        }

        private void OnTryAgainButtonClick(object sender, RoutedEventArgs e)
        {
            RunUpdateCheck();
            AnalyticsService.SendEvent(new RetryButtonClickedEvent());
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
            AnalyticsService.SendEvent(new CloseButtonClickedEvent());
        }

        private void ShowCheckForUpdateStatus()
        {
            statusCheckForUpdate.IsVisible = true;
            statusUpdateAvailable.IsVisible = false;
            statusUpdateUnavailable.IsVisible = false;
            statusConnectionError.IsVisible = false;

            buttonCancel.IsVisible = true;
            buttonUpdate.IsVisible = false;
            buttonTryAgain.IsVisible = false;
            buttonClose.IsVisible = false;

            textUpdateStatus.Text = LocalizationService.GetTerm(Localization.WAITING_FOR_SERVER_RESPONSE);
        }

        private void ShowUpdateAvailableStatus()
        {
            statusUpdateAvailable.IsVisible = true;
            statusCheckForUpdate.IsVisible = false;
            statusUpdateUnavailable.IsVisible = false;
            statusConnectionError.IsVisible = false;

            buttonUpdate.IsVisible = true;
            buttonCancel.IsVisible = false;
            buttonTryAgain.IsVisible = false;
            buttonClose.IsVisible = false;

            textUpdateStatus.Text = LocalizationService.GetTerm(Localization.UPDATE_AVAILABLE);
        }

        private void ShowUpdateUnavailableStatus()
        {
            statusUpdateUnavailable.IsVisible = true;
            statusCheckForUpdate.IsVisible = false;
            statusUpdateAvailable.IsVisible = false;
            statusConnectionError.IsVisible = false;

            buttonClose.IsVisible = true;
            buttonCancel.IsVisible = false;
            buttonUpdate.IsVisible = false;
            buttonTryAgain.IsVisible = false;

            textUpdateStatus.Text = LocalizationService.GetTerm(Localization.YOU_HAVE_LATEST_VERSION);
        }

        private void ShowConnectionErrorStatus()
        {
            statusConnectionError.IsVisible = true;
            statusCheckForUpdate.IsVisible = false;
            statusUpdateAvailable.IsVisible = false;
            statusUpdateUnavailable.IsVisible = false;

            buttonTryAgain.IsVisible = true;
            buttonCancel.IsVisible = false;
            buttonUpdate.IsVisible = false;
            buttonClose.IsVisible = false;

            textUpdateStatus.Text = LocalizationService.GetTerm(Localization.CANT_CONNECT_TO_SERVER);
        }
    }
}
