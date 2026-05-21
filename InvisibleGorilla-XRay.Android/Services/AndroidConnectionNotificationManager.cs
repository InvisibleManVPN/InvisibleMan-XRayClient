using System;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Net;
using Android.OS;
using InvisibleGorillaXRay.Core;

namespace InvisibleGorillaXRay.Android.Services
{
    internal sealed class AndroidConnectionNotificationText
    {
        public string AppName { get; init; } = string.Empty;
        public string ChannelName { get; init; } = string.Empty;
        public string ChannelDescription { get; init; } = string.Empty;
        public string StateStarting { get; init; } = string.Empty;
        public string StateRunning { get; init; } = string.Empty;
        public string StateStopping { get; init; } = string.Empty;
        public string StateStopped { get; init; } = string.Empty;
        public string ConfigLabel { get; init; } = string.Empty;
        public string EndpointLabel { get; init; } = string.Empty;
        public string ListenerLabel { get; init; } = string.Empty;
        public string ProtocolLabel { get; init; } = string.Empty;
        public string TrafficLabel { get; init; } = string.Empty;
        public string SpeedLabel { get; init; } = string.Empty;
        public string UptimeLabel { get; init; } = string.Empty;
        public string UnknownEndpoint { get; init; } = string.Empty;
    }

    internal sealed class AndroidConnectionNotificationSession
    {
        public string ConfigName { get; init; } = string.Empty;
        public string Endpoint { get; init; } = string.Empty;
        public string Listener { get; init; } = string.Empty;
        public string Protocol { get; init; } = string.Empty;
        public AndroidConnectionNotificationText Text { get; init; } = new();
    }

    internal enum AndroidConnectionNotificationState
    {
        Starting,
        Running,
        Stopping,
        Stopped
    }

    internal static class AndroidConnectionNotificationManager
    {
        private const string ChannelId = "invisiblegorilla.connection.status";
        private const int NotificationId = 42042;
        internal const int ForegroundNotificationId = NotificationId;
        private static readonly object SyncRoot = new();

        private static Timer? updateTimer;
        private static AndroidConnectionNotificationSession? currentSession;
        private static AndroidConnectionNotificationState currentState;
        private static DateTime startedAtUtc;
        private static DateTime lastSampleUtc;
        private static long baseRxBytes;
        private static long baseTxBytes;
        private static long lastRxBytes;
        private static long lastTxBytes;
        private static bool channelCreated;

        public static void ShowStarting(AndroidConnectionNotificationSession session)
        {
            lock (SyncRoot)
            {
                currentSession = session;
                currentState = AndroidConnectionNotificationState.Starting;
                startedAtUtc = DateTime.UtcNow;
                DiagnosticLog.Write("AndroidConnectionNotification", $"ShowStarting config={session.ConfigName}");

                long rxBytes = ReadUidRxBytes();
                long txBytes = ReadUidTxBytes();

                baseRxBytes = rxBytes;
                baseTxBytes = txBytes;
                lastRxBytes = rxBytes;
                lastTxBytes = txBytes;
                lastSampleUtc = startedAtUtc;

                EnsureChannelLocked();
                EnsureTimerLocked();
                PublishNotificationLocked();
            }
        }

        public static void MarkRunning()
        {
            lock (SyncRoot)
            {
                if (currentSession == null)
                    return;

                currentState = AndroidConnectionNotificationState.Running;
                DiagnosticLog.Write("AndroidConnectionNotification", $"MarkRunning config={currentSession.ConfigName}");
                PublishNotificationLocked();
            }
        }

        public static void MarkStopping()
        {
            lock (SyncRoot)
            {
                if (currentSession == null)
                    return;

                currentState = AndroidConnectionNotificationState.Stopping;
                DiagnosticLog.Write("AndroidConnectionNotification", $"MarkStopping config={currentSession.ConfigName}");
                PublishNotificationLocked();
            }
        }

        public static void MarkStopped()
        {
            lock (SyncRoot)
            {
                if (currentSession == null)
                    return;

                updateTimer?.Dispose();
                updateTimer = null;
                currentState = AndroidConnectionNotificationState.Stopped;
                DiagnosticLog.Write("AndroidConnectionNotification", $"MarkStopped config={currentSession.ConfigName}");
                PublishNotificationLocked();
            }
        }

        public static void Stop()
        {
            lock (SyncRoot)
            {
                updateTimer?.Dispose();
                updateTimer = null;
                DiagnosticLog.Write("AndroidConnectionNotification", "Stop and clear notification state");
                currentSession = null;
                CancelNotificationLocked();
            }
        }

        internal static Notification BuildForegroundNotification(Context context)
        {
            lock (SyncRoot)
            {
                EnsureChannelLocked();
                return currentSession == null
                    ? BuildFallbackNotification(context)
                    : BuildNotificationLocked(context);
            }
        }

        private static void EnsureChannelLocked()
        {
            if (channelCreated || Build.VERSION.SdkInt < BuildVersionCodes.O)
                return;

            Context? context = global::Android.App.Application.Context;
            if (context?.GetSystemService(Context.NotificationService) is not NotificationManager manager)
                return;

            string channelName = currentSession?.Text.ChannelName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(channelName))
                channelName = "Connection status";

            string channelDescription = currentSession?.Text.ChannelDescription ?? string.Empty;
            if (string.IsNullOrWhiteSpace(channelDescription))
                channelDescription = "Shows current proxy connection status and traffic.";

            NotificationChannel channel = new(
                ChannelId,
                channelName,
                NotificationImportance.Low)
            {
                Description = channelDescription,
                LockscreenVisibility = NotificationVisibility.Public
            };

            manager.CreateNotificationChannel(channel);
            channelCreated = true;
        }

        private static void EnsureTimerLocked()
        {
            updateTimer?.Dispose();
            updateTimer = new Timer(
                callback: static _ => OnTimerTick(),
                state: null,
                dueTime: TimeSpan.FromSeconds(2),
                period: TimeSpan.FromSeconds(2));
        }

        private static void OnTimerTick()
        {
            lock (SyncRoot)
            {
                if (currentSession == null)
                    return;

                PublishNotificationLocked();
            }
        }

        private static void PublishNotificationLocked()
        {
            Context? context = global::Android.App.Application.Context;
            if (context == null || currentSession == null)
                return;

            if (!CanPostNotifications(context))
                return;

            if (context.GetSystemService(Context.NotificationService) is not NotificationManager manager)
                return;

            try
            {
                manager.Notify(NotificationId, BuildNotificationLocked(context));
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidConnectionNotification.Notify", ex);
            }
        }

        private static void CancelNotificationLocked()
        {
            Context? context = global::Android.App.Application.Context;
            if (context?.GetSystemService(Context.NotificationService) is not NotificationManager manager)
                return;

            try
            {
                manager.Cancel(NotificationId);
            }
            catch (Exception ex)
            {
                DiagnosticLog.WriteException("AndroidConnectionNotification.Cancel", ex);
            }
        }

        private static Notification BuildNotificationLocked(Context context)
        {
            AndroidConnectionNotificationSession session = currentSession ?? new AndroidConnectionNotificationSession();
            DateTime now = DateTime.UtcNow;

            long currentRxBytes = ReadUidRxBytes();
            long currentTxBytes = ReadUidTxBytes();

            long totalRxBytes = Math.Max(0, currentRxBytes - baseRxBytes);
            long totalTxBytes = Math.Max(0, currentTxBytes - baseTxBytes);

            double elapsedSampleSeconds = Math.Max(1d, (now - lastSampleUtc).TotalSeconds);
            long rxSpeedBytes = currentState == AndroidConnectionNotificationState.Running
                ? (long)Math.Max(0, (currentRxBytes - lastRxBytes) / elapsedSampleSeconds)
                : 0;
            long txSpeedBytes = currentState == AndroidConnectionNotificationState.Running
                ? (long)Math.Max(0, (currentTxBytes - lastTxBytes) / elapsedSampleSeconds)
                : 0;

            lastSampleUtc = now;
            lastRxBytes = currentRxBytes;
            lastTxBytes = currentTxBytes;

            string stateText = GetStateText(session.Text);
            string contentText = currentState == AndroidConnectionNotificationState.Running
                ? $"{stateText} - {session.ConfigName} - RX {FormatBytes(rxSpeedBytes)}/s TX {FormatBytes(txSpeedBytes)}/s"
                : $"{stateText} - {session.ConfigName}";

            StringBuilder expandedText = new();
            expandedText.AppendLine(stateText);
            expandedText.AppendLine($"{session.Text.ConfigLabel}: {session.ConfigName}");
            expandedText.AppendLine($"{session.Text.ListenerLabel}: {session.Listener}");
            expandedText.AppendLine($"{session.Text.TrafficLabel}: RX {FormatBytes(totalRxBytes)} / TX {FormatBytes(totalTxBytes)}");
            expandedText.AppendLine($"{session.Text.SpeedLabel}: RX {FormatBytes(rxSpeedBytes)}/s / TX {FormatBytes(txSpeedBytes)}/s");
            expandedText.Append($"{session.Text.UptimeLabel}: {FormatElapsed(now - startedAtUtc)}");

            Notification.Builder builder = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? new Notification.Builder(context, ChannelId)
                : new Notification.Builder(context);

            builder
                .SetContentTitle(session.Text.AppName)
                .SetContentText(contentText)
                .SetStyle(new Notification.BigTextStyle().BigText(expandedText.ToString()))
                .SetSmallIcon(Resource.Drawable.ic_notification_connection)
                .SetContentIntent(CreateLaunchPendingIntent(context))
                .SetOnlyAlertOnce(true)
                .SetOngoing(currentState != AndroidConnectionNotificationState.Stopped)
                .SetShowWhen(false)
                .SetVisibility(NotificationVisibility.Public);

            return builder.Build();
        }

        private static Notification BuildFallbackNotification(Context context)
        {
            Notification.Builder builder = Build.VERSION.SdkInt >= BuildVersionCodes.O
                ? new Notification.Builder(context, ChannelId)
                : new Notification.Builder(context);

            builder
                .SetContentTitle("Invisible Gorilla XRay")
                .SetContentText("Preparing Android VPN tunnel...")
                .SetSmallIcon(Resource.Drawable.ic_notification_connection)
                .SetContentIntent(CreateLaunchPendingIntent(context))
                .SetOnlyAlertOnce(true)
                .SetOngoing(true)
                .SetShowWhen(false)
                .SetVisibility(NotificationVisibility.Public);

            return builder.Build();
        }

        private static PendingIntent CreateLaunchPendingIntent(Context context)
        {
            Intent intent = new Intent(context, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop | ActivityFlags.NewTask);

            return PendingIntent.GetActivity(
                context,
                requestCode: 0,
                intent,
                PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)!;
        }

        private static bool CanPostNotifications(Context context)
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
                return true;

            return context.CheckSelfPermission(global::Android.Manifest.Permission.PostNotifications) == Permission.Granted;
        }

        private static long ReadUidRxBytes()
        {
            long bytes = TrafficStats.GetUidRxBytes(global::Android.OS.Process.MyUid());
            return bytes < 0 ? 0 : bytes;
        }

        private static long ReadUidTxBytes()
        {
            long bytes = TrafficStats.GetUidTxBytes(global::Android.OS.Process.MyUid());
            return bytes < 0 ? 0 : bytes;
        }

        private static string GetStateText(AndroidConnectionNotificationText text)
        {
            return currentState switch
            {
                AndroidConnectionNotificationState.Starting => text.StateStarting,
                AndroidConnectionNotificationState.Running => text.StateRunning,
                AndroidConnectionNotificationState.Stopping => text.StateStopping,
                AndroidConnectionNotificationState.Stopped => text.StateStopped,
                _ => text.StateRunning
            };
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = Math.Max(0, bytes);
            int unitIndex = 0;

            while (value >= 1024d && unitIndex < units.Length - 1)
            {
                value /= 1024d;
                unitIndex++;
            }

            string pattern = value >= 100d || unitIndex == 0 ? "0" : "0.0";
            return $"{value.ToString(pattern, System.Globalization.CultureInfo.InvariantCulture)} {units[unitIndex]}";
        }

        private static string FormatElapsed(TimeSpan elapsed)
        {
            if (elapsed.TotalHours >= 1)
                return elapsed.ToString(@"hh\:mm\:ss", System.Globalization.CultureInfo.InvariantCulture);

            return elapsed.ToString(@"mm\:ss", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
