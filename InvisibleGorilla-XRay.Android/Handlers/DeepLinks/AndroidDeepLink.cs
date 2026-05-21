using System;
using System.Collections.Generic;
using Avalonia.Threading;

namespace InvisibleGorillaXRay.Android.Handlers.DeepLinks
{
    using InvisibleGorillaXRay.Handlers.DeepLinks;

    public sealed class AndroidDeepLink : IDeepLink
    {
        public void Register()
        {
        }
    }

    internal enum AndroidImportKind
    {
        ConfigLink,
        SubscriptionLink,
        ConfigFile
    }

    internal sealed class AndroidPendingImport
    {
        public AndroidPendingImport(AndroidImportKind kind, string value, string? displayName = null)
        {
            Kind = kind;
            Value = value;
            DisplayName = displayName;
        }

        public AndroidImportKind Kind { get; }
        public string Value { get; }
        public string? DisplayName { get; }
    }

    public static class AndroidDeepLinkDispatcher
    {
        private static readonly object SyncRoot = new();
        private static readonly Queue<AndroidPendingImport> PendingImports = new();
        private static Action<AndroidPendingImport>? onImportReceived;

        public static Action<string> OnReceiveArg = _ => { };

        internal static void Register(Action<AndroidPendingImport> handler)
        {
            List<AndroidPendingImport> queuedImports;

            lock (SyncRoot)
            {
                onImportReceived += handler;
                queuedImports = new List<AndroidPendingImport>(PendingImports);
                PendingImports.Clear();
            }

            foreach (AndroidPendingImport pendingImport in queuedImports)
                PostImport(handler, pendingImport);
        }

        internal static void Unregister(Action<AndroidPendingImport> handler)
        {
            lock (SyncRoot)
                onImportReceived -= handler;
        }

        internal static bool DispatchExternalValue(string? value)
        {
            string normalizedValue = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedValue))
                return false;

            if (normalizedValue.StartsWith(InvisibleGorillaXRay.Values.DeepLink.CONFIG, StringComparison.OrdinalIgnoreCase))
            {
                OnReceiveArg(normalizedValue);
                return DispatchConfigLink(normalizedValue[InvisibleGorillaXRay.Values.DeepLink.CONFIG.Length..]);
            }

            if (normalizedValue.StartsWith(InvisibleGorillaXRay.Values.DeepLink.CONFIG_DATA, StringComparison.OrdinalIgnoreCase))
            {
                OnReceiveArg(normalizedValue);
                return DispatchConfigLink(normalizedValue);
            }

            if (normalizedValue.StartsWith(InvisibleGorillaXRay.Values.DeepLink.SUBSCRIPTION, StringComparison.OrdinalIgnoreCase))
            {
                OnReceiveArg(normalizedValue);
                return DispatchSubscriptionLink(normalizedValue[InvisibleGorillaXRay.Values.DeepLink.SUBSCRIPTION.Length..]);
            }

            if (IsConfigLink(normalizedValue))
                return DispatchConfigLink(normalizedValue);

            if (IsSubscriptionLink(normalizedValue))
                return DispatchSubscriptionLink(normalizedValue);

            return false;
        }

        internal static bool DispatchConfigFile(string? displayName, string? content)
        {
            string normalizedContent = content?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedContent))
                return false;

            Dispatch(new AndroidPendingImport(
                kind: AndroidImportKind.ConfigFile,
                value: normalizedContent,
                displayName: displayName));

            return true;
        }

        private static bool DispatchConfigLink(string? link)
        {
            string normalizedLink = link?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedLink))
                return false;

            Dispatch(new AndroidPendingImport(AndroidImportKind.ConfigLink, normalizedLink));
            return true;
        }

        private static bool DispatchSubscriptionLink(string? link)
        {
            string normalizedLink = link?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedLink))
                return false;

            Dispatch(new AndroidPendingImport(AndroidImportKind.SubscriptionLink, normalizedLink));
            return true;
        }

        private static void Dispatch(AndroidPendingImport pendingImport)
        {
            Action<AndroidPendingImport>? handler;

            lock (SyncRoot)
            {
                handler = onImportReceived;
                if (handler == null)
                {
                    PendingImports.Enqueue(pendingImport);
                    return;
                }
            }

            PostImport(handler, pendingImport);
        }

        private static void PostImport(Action<AndroidPendingImport> handler, AndroidPendingImport pendingImport)
        {
            Dispatcher.UIThread.Post(() => handler(pendingImport));
        }

        private static bool IsConfigLink(string value)
        {
            return value.StartsWith("vless://", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("vmess://", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("trojan://", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("ss://", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSubscriptionLink(string value)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri)
                && (uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    || uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase));
        }
    }
}
