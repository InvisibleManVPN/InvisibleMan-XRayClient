using System;
using System.Windows;
using System.ComponentModel;

namespace InvisibleManXRay
{
    using Models;
    using Values;
    using Services;
    using Services.Analytics.UpdateWindow;

    public partial class UpdateWindow : Window
    {
        private Func<Status> checkForUpdate;
        private Action onUpdateClick;

        private BackgroundWorker updateWorker;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();
        private AnalyticsService AnalyticsService => ServiceLocator.Get<AnalyticsService>();

        public UpdateWindow()
        {
            InitializeComponent();
            InitializeUpdateWorker();
            updateWorker.RunWorkerAsync();

            void InitializeUpdateWorker()
            {
                updateWorker = new BackgroundWorker();

                updateWorker.DoWork+= (sender, e) => {
                    Dispatcher.BeginInvoke(new Action(delegate {
                        ShowCheckForUpdateStatus();
                    }));

                    Status updateStatus = checkForUpdate.Invoke();

                    Dispatcher.BeginInvoke(new Action(delegate {
                        CheckUpdateResponse(updateStatus);
                    }));
                };

                void CheckUpdateResponse(Status updateStatus)
                {
                    switch(updateStatus.Code)
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
                        default:
                            break;
                    }
                }

                void ShowCheckForUpdateStatus()
                {
                    SetActiveCheckForUpdateStatus(true);
                    SetActiveConnectionErrorStatus(false);
                    SetActiveUpdateAvailableStatus(false);
                    SetActiveUpdateUnavailableStatus(false);

                    textUpdateStatus.Content = LocalizationService.GetTerm(Localization.WAITING_FOR_SERVER_RESPONSE);
                }

                void ShowUpdateAvailableStatus()
                {
                    SetActiveUpdateAvailableStatus(true);
                    SetActiveUpdateUnavailableStatus(false);
                    SetActiveConnectionErrorStatus(false);
                    SetActiveCheckForUpdateStatus(false);

                    textUpdateStatus.Content = Message.UPDATE_AVAILABLE;
                }

                void ShowUpdateUnavailableStatus()
                {
                    SetActiveUpdateUnavailableStatus(true);
                    SetActiveUpdateAvailableStatus(false);
                    SetActiveConnectionErrorStatus(false);
                    SetActiveCheckForUpdateStatus(false);

                    textUpdateStatus.Content = Message.YOU_HAVE_LATEST_VERSION;
                }

                void ShowConnectionErrorStatus()
                {
                    SetActiveConnectionErrorStatus(true);
                    SetActiveUpdateAvailableStatus(false);
                    SetActiveUpdateUnavailableStatus(false);
                    SetActiveCheckForUpdateStatus(false);

                    textUpdateStatus.Content = Message.CANT_CONNECT_TO_SERVER;
                }
            }
        }

        public void Setup(Func<Status> checkForUpdate, Action onUpdateClick)
        {
            this.checkForUpdate = checkForUpdate;
            this.onUpdateClick = onUpdateClick;
        }

        private void OnUpdateButtonClick(object sender, RoutedEventArgs e)
        {
            onUpdateClick.Invoke();
            AnalyticsService.SendEvent(new UpdateButtonClickedEvent());
        }

        private void OnTryAgainButtonClick(object sender, RoutedEventArgs e)
        {
            updateWorker.RunWorkerAsync();
            AnalyticsService.SendEvent(new RetryButtonClickedEvent());
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
            AnalyticsService.SendEvent(new CloseButtonClickedEvent());
        }

        private void SetActiveCheckForUpdateStatus(bool isShow)
        {
            Visibility visibility = isShow ? Visibility.Visible : Visibility.Collapsed;
            statusCheckForUpdate.Visibility = visibility;
            buttonCancel.Visibility = visibility;
        }

        private void SetActiveUpdateAvailableStatus(bool isShow)
        {
            Visibility visibility = isShow ? Visibility.Visible : Visibility.Collapsed;
            statusUpdateAvailable.Visibility = visibility;
            buttonUpdate.Visibility = visibility;
        }

        private void SetActiveUpdateUnavailableStatus(bool isShow)
        {
            Visibility visibility = isShow ? Visibility.Visible : Visibility.Collapsed;
            statusUpdateUnavailable.Visibility = visibility;
            buttonClose.Visibility = visibility;
        }

        private void SetActiveConnectionErrorStatus(bool isShow)
        {
            Visibility visibility = isShow ? Visibility.Visible : Visibility.Collapsed;
            statusConnectionError.Visibility = visibility;
            buttonTryAgain.Visibility = visibility;
        }
    }
}
