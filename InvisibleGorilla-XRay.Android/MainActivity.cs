using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Avalonia;
using Avalonia.Android;
using System.Threading;
using System.Threading.Tasks;

namespace InvisibleGorillaXRay.Android
{
    using InvisibleGorillaXRay.Android.Handlers.DeepLinks;
    using InvisibleGorillaXRay.Core;

    [Activity(
        Label = "Invisible Gorilla XRay",
        Theme = "@style/Theme.AppCompat.DayNight.NoActionBar",
        MainLauncher = true,
        LaunchMode = LaunchMode.SingleTask,
        WindowSoftInputMode = SoftInput.AdjustResize,
        ConfigurationChanges =
            ConfigChanges.Orientation |
            ConfigChanges.ScreenSize |
            ConfigChanges.UiMode |
            ConfigChanges.ScreenLayout |
            ConfigChanges.SmallestScreenSize |
            ConfigChanges.Density)]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "invxray")]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "vless")]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "vmess")]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "trojan")]
    [IntentFilter(
        new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = "ss")]
    [IntentFilter(
        new[] { Intent.ActionSend },
        Categories = new[] { Intent.CategoryDefault },
        DataMimeType = "text/plain")]
    public class MainActivity : AvaloniaMainActivity<App>
    {
        private const int NotificationPermissionRequestCode = 1001;
        private const int VpnPermissionRequestCode = 1002;
        private static readonly object ActivitySync = new();
        private static MainActivity? currentActivity;
        private static TaskCompletionSource<bool>? vpnPermissionRequest;
        private static int globalHandlersRegistered;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            RegisterGlobalExceptionHandlersOnce();

            try
            {
                base.OnCreate(savedInstanceState);
                SetCurrentActivity(this);
                RequestNotificationPermissionIfNeeded();
                DispatchIncomingIntent(Intent);
                DiagnosticLog.Write("MainActivity", $"OnCreate completed, intent={Intent?.Action ?? "<null>"}");
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("MainActivity.OnCreate", ex);
                throw;
            }
        }

        private static void RegisterGlobalExceptionHandlersOnce()
        {
            if (Interlocked.Exchange(ref globalHandlersRegistered, 1) == 1)
                return;

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                try
                {
                    Exception? ex = args.ExceptionObject as Exception;
                    if (ex != null)
                        DiagnosticLog.WriteException("AppDomain.UnhandledException", ex);
                    else
                        DiagnosticLog.Write("AppDomain.UnhandledException", $"Unknown error: {args.ExceptionObject}");
                }
                catch
                {
                    // Crash handler must never throw.
                }
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                try
                {
                    DiagnosticLog.WriteException("TaskScheduler.UnobservedTaskException", args.Exception);
                    args.SetObserved();
                }
                catch
                {
                    // Crash handler must never throw.
                }
            };

            try
            {
                global::Java.Lang.Thread.DefaultUncaughtExceptionHandler = new JavaUncaughtExceptionHandler();
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("MainActivity.RegisterJavaUncaughtHandler", ex);
            }

            DiagnosticLog.Write("MainActivity", "Global exception handlers installed");
        }

        private sealed class JavaUncaughtExceptionHandler : global::Java.Lang.Object, global::Java.Lang.Thread.IUncaughtExceptionHandler
        {
            private readonly global::Java.Lang.Thread.IUncaughtExceptionHandler? previous;

            public JavaUncaughtExceptionHandler()
            {
                previous = global::Java.Lang.Thread.DefaultUncaughtExceptionHandler;
            }

            public void UncaughtException(global::Java.Lang.Thread? t, global::Java.Lang.Throwable? e)
            {
                try
                {
                    string threadName = t?.Name ?? "<unknown>";
                    string typeName = e?.GetType().FullName ?? "<unknown>";
                    string message = e?.Message ?? string.Empty;
                    DiagnosticLog.Write("Java.UncaughtException", $"thread={threadName}, type={typeName}, message={message}");
                    if (e != null)
                        DiagnosticLog.Write("Java.UncaughtException", $"stack={e.ToString()}");
                }
                catch
                {
                    // Crash handler must never throw.
                }

                previous?.UncaughtException(t, e);
            }
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            SetCurrentActivity(this);

            if (intent == null)
                return;

            Intent = intent;
            DispatchIncomingIntent(intent);
        }

        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            return base.CustomizeAppBuilder(builder)
                .LogToTrace();
        }

        protected override void OnResume()
        {
            base.OnResume();
            SetCurrentActivity(this);
        }

        protected override void OnDestroy()
        {
            lock (ActivitySync)
            {
                if (ReferenceEquals(currentActivity, this))
                    currentActivity = null;
            }

            base.OnDestroy();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            if (requestCode == VpnPermissionRequestCode)
            {
                TaskCompletionSource<bool>? completion;
                lock (ActivitySync)
                {
                    completion = vpnPermissionRequest;
                    vpnPermissionRequest = null;
                }

                bool granted = resultCode == Result.Ok || global::Android.Net.VpnService.Prepare(this) == null;
                completion?.TrySetResult(granted);
                return;
            }

            base.OnActivityResult(requestCode, resultCode, data);
        }

        internal static Task<bool> EnsureVpnPreparedAsync()
        {
            MainActivity? activity = GetCurrentActivity();
            if (activity == null)
                return Task.FromResult(false);

            Intent? intent = global::Android.Net.VpnService.Prepare(activity);
            if (intent == null)
                return Task.FromResult(true);

            lock (ActivitySync)
            {
                if (vpnPermissionRequest != null)
                    return vpnPermissionRequest.Task;

                vpnPermissionRequest = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            activity.RunOnUiThread(() =>
            {
                try
                {
                    activity.StartActivityForResult(intent, VpnPermissionRequestCode);
                }
                catch (Exception ex)
                {
                    DiagnosticLog.WriteException("MainActivity.StartVpnConsent", ex);

                    lock (ActivitySync)
                    {
                        vpnPermissionRequest?.TrySetResult(false);
                        vpnPermissionRequest = null;
                    }
                }
            });

            return vpnPermissionRequest.Task;
        }

        private static void DispatchIncomingIntent(Intent? intent)
        {
            if (intent == null)
                return;

            try
            {
                if (TryDispatchViewIntent(intent))
                    return;

                TryDispatchSharedText(intent);
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("MainActivity.DispatchIncomingIntent", ex);
            }
        }

        private static bool TryDispatchViewIntent(Intent intent)
        {
            if (!string.Equals(intent.Action, Intent.ActionView, StringComparison.Ordinal))
                return false;

            return AndroidDeepLinkDispatcher.DispatchExternalValue(intent.DataString);
        }

        private static bool TryDispatchSharedText(Intent intent)
        {
            if (!string.Equals(intent.Action, Intent.ActionSend, StringComparison.Ordinal))
                return false;

            string? sharedText = intent.GetStringExtra(Intent.ExtraText);
            return AndroidDeepLinkDispatcher.DispatchExternalValue(sharedText);
        }

        private void RequestNotificationPermissionIfNeeded()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
                return;

            if (CheckSelfPermission(global::Android.Manifest.Permission.PostNotifications) == Permission.Granted)
                return;

            RequestPermissions(
                new[] { global::Android.Manifest.Permission.PostNotifications },
                NotificationPermissionRequestCode);
        }

        private static MainActivity? GetCurrentActivity()
        {
            lock (ActivitySync)
            {
                return currentActivity;
            }
        }

        internal static MainActivity? CurrentActivity => GetCurrentActivity();

        private static void SetCurrentActivity(MainActivity activity)
        {
            lock (ActivitySync)
            {
                currentActivity = activity;
            }
        }
    }
}
