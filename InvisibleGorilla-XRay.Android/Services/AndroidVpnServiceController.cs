using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Content;
using Android.OS;
using InvisibleGorillaXRay.Models;

namespace InvisibleGorillaXRay.Android.Services
{
    internal sealed class AndroidVpnStartOptions
    {
        public int ProxyPort { get; init; }
        public string ProxyUsername { get; init; } = string.Empty;
        public string ProxyPassword { get; init; } = string.Empty;
        public bool UdpEnabled { get; init; }
        public string TunAddress { get; init; } = string.Empty;
        public string Dns { get; init; } = string.Empty;
        public string SessionName { get; init; } = "Invisible Gorilla XRay";
        public AppRulesMode AppRulesMode { get; init; } = AppRulesMode.ALL_APPS;
        public IReadOnlyList<string> AppPackages { get; init; } = Array.Empty<string>();
    }

    internal static class AndroidVpnServiceController
    {
        private static readonly object SyncRoot = new();
        private static TaskCompletionSource<Status>? pendingStart;
        private static bool isRunning;
        private static bool isStopping;
        private static string lastError = string.Empty;

        public static Status Start(AndroidVpnStartOptions options)
        {
            Context? context = global::Android.App.Application.Context;
            if (context == null)
                return CreateError("Android application context is unavailable.");

            if (global::Android.Net.VpnService.Prepare(context) != null)
                return CreateError("Android VPN permission has not been granted.");

            TaskCompletionSource<Status> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (SyncRoot)
            {
                pendingStart = completion;
                lastError = string.Empty;
                isStopping = false;
            }

            try
            {
                Intent intent = AndroidVpnService.CreateStartIntent(context, options);
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    context.StartForegroundService(intent);
                else
                    context.StartService(intent);
            }
            catch (Exception ex)
            {
                lock (SyncRoot)
                {
                    if (pendingStart == completion)
                        pendingStart = null;

                    lastError = ex.Message;
                }

                return CreateError(ex.Message);
            }

            if (!completion.Task.Wait(TimeSpan.FromSeconds(15)))
            {
                lock (SyncRoot)
                {
                    if (pendingStart == completion)
                        pendingStart = null;

                    lastError = "Timed out while waiting for the Android VPN service to start.";
                }

                return CreateError(lastError);
            }

            return completion.Task.Result;
        }

        public static void Stop()
        {
            Context? context = global::Android.App.Application.Context;
            if (context == null)
                return;

            lock (SyncRoot)
            {
                if (!isRunning || isStopping)
                    return;

                isStopping = true;
            }

            try
            {
                context.StartService(AndroidVpnService.CreateStopIntent(context));
            }
            catch (Exception ex)
            {
                lock (SyncRoot)
                {
                    lastError = ex.Message;
                    isRunning = false;
                }
            }
        }

        public static bool IsRunning
        {
            get
            {
                lock (SyncRoot)
                    return isRunning;
            }
        }

        public static bool IsStopping
        {
            get
            {
                lock (SyncRoot)
                    return isStopping;
            }
        }

        public static string LastError
        {
            get
            {
                lock (SyncRoot)
                    return lastError;
            }
        }

        internal static void NotifyStarted()
        {
            lock (SyncRoot)
            {
                isRunning = true;
                isStopping = false;
                lastError = string.Empty;
                pendingStart?.TrySetResult(new Status(Code.SUCCESS, SubCode.SUCCESS, string.Empty));
                pendingStart = null;
            }
        }

        internal static void NotifyStartFailed(string message)
        {
            lock (SyncRoot)
            {
                isRunning = false;
                isStopping = false;
                lastError = string.IsNullOrWhiteSpace(message)
                    ? "Android VPN service failed to start."
                    : message;
                pendingStart?.TrySetResult(CreateError(lastError));
                pendingStart = null;
            }
        }

        internal static void NotifyStopped(string? message = null)
        {
            lock (SyncRoot)
            {
                isRunning = false;
                isStopping = false;
                if (!string.IsNullOrWhiteSpace(message))
                    lastError = message;
            }
        }

        private static Status CreateError(string message)
        {
            return new Status(
                code: Code.ERROR,
                subCode: SubCode.CANT_TUNNEL,
                content: message);
        }
    }
}
