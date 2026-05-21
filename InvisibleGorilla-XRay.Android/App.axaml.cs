using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace InvisibleGorillaXRay.Android
{
    using InvisibleGorillaXRay.Android.Handlers;
    using InvisibleGorillaXRay.Android.Managers;
    using InvisibleGorillaXRay.Android.Platforms;
    using InvisibleGorillaXRay.Android.Views;

    public partial class App : Application
    {
        private AndroidAppManager? appManager;

        public override void Initialize()
        {
            try
            {
                AvaloniaXamlLoader.Load(this);
                InvisibleGorillaXRay.Core.DiagnosticLog.Write("AndroidApp", "App.Initialize: AvaloniaXamlLoader.Load completed");
            }
            catch (Exception ex)
            {
                InvisibleGorillaXRay.Core.DiagnosticLog.WriteException("AndroidApp.Initialize", ex);
                throw;
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            try
            {
                if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
                {
                    InvisibleGorillaXRay.Core.DiagnosticLog.Write("AndroidApp", "Single view lifetime detected");

                    AndroidAppStorage.ConfigureAppRoot();
                    InvisibleGorillaXRay.Core.DiagnosticLog.Write(
                        "AndroidApp",
                        $"App root configured: {InvisibleGorillaXRay.Values.Directory.ROOT}");

                    AndroidAppStorage.EnsureRuntimeAssets();
                    InvisibleGorillaXRay.Core.DiagnosticLog.Write("AndroidApp", "Runtime assets prepared");

                    appManager = new AndroidAppManager();
                    InvisibleGorillaXRay.Core.DiagnosticLog.Write("AndroidApp", "App manager created");

                    appManager.Initialize();
                    InvisibleGorillaXRay.Core.DiagnosticLog.Write("AndroidApp", "App manager initialized");

                    MainView mainView = new MainView();
                    InvisibleGorillaXRay.Core.DiagnosticLog.Write("AndroidApp", "MainView constructed");

                    singleView.MainView = mainView;
                    InvisibleGorillaXRay.Core.DiagnosticLog.Write("AndroidApp", "MainView assigned to lifetime");

                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            appManager.HandlersManager
                                .GetHandler<AndroidLocalizationHandler>()
                                .TryApplyCurrentLanguage();
                            mainView.Setup(appManager);
                            InvisibleGorillaXRay.Core.DiagnosticLog.Write("AndroidApp", "MainView setup completed");
                        }
                        catch (Exception ex)
                        {
                            InvisibleGorillaXRay.Core.DiagnosticLog.WriteException("AndroidApp.SetupPost", ex);
                        }
                    }, DispatcherPriority.ApplicationIdle);
                }
            }
            catch (Exception ex)
            {
                InvisibleGorillaXRay.Core.DiagnosticLog.WriteException("AndroidApp", ex);
                throw;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
