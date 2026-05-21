using System;

namespace InvisibleGorillaXRay.Handlers
{
    using DeepLinks;
    using Values;

    public class DeepLinkHandler : Handler
    {
        private IDeepLink deepLink;

        private Action<string> onConfigLinkFetched;
        private Action<string> onSubscriptionLinkFetched;

        public DeepLinkHandler()
        {
            InitializeDeepLink();
        }

        public void Setup(
            ref Action<string> onReceiveArg,
            Action<string> onConfigLinkFetched,
            Action<string> onSubscriptionLinkFetched
        )
        {
            onReceiveArg = TryFetchLink;
            this.onConfigLinkFetched = onConfigLinkFetched;
            this.onSubscriptionLinkFetched = onSubscriptionLinkFetched;
        }

        private void InitializeDeepLink()
        {
            this.deepLink = GetDeepLink();
            deepLink.Register();
        }

        private IDeepLink GetDeepLink()
        {
            WindowsDeepLink windowsDeepLink = new WindowsDeepLink();
            return windowsDeepLink;
        }

        private void TryFetchLink(string arg)
        {
            if (IsValidConfigDataLink())
                onConfigLinkFetched.Invoke(GetConfigDataLink());
            else if (IsValidConfigLink())
                onConfigLinkFetched.Invoke(GetConfigLink());
            else if (IsValidSubscriptionLink())
                onSubscriptionLinkFetched.Invoke(GetSubscriptionLink());

            bool IsValidConfigDataLink()
            {
                return arg.StartsWith(DeepLink.CONFIG_DATA, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(GetConfigDataLink());
            }

            bool IsValidConfigLink()
            {
                return arg.StartsWith(DeepLink.CONFIG, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(GetConfigLink());
            }

            bool IsValidSubscriptionLink()
            {
                return arg.StartsWith(DeepLink.SUBSCRIPTION, StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrEmpty(GetSubscriptionLink());
            }

            string GetConfigDataLink() => Uri.UnescapeDataString(
                arg.Substring(DeepLink.CONFIG_DATA.Length).Trim());

            string GetConfigLink() => arg.Substring(DeepLink.CONFIG.Length).Trim();

            string GetSubscriptionLink() => arg.Substring(DeepLink.SUBSCRIPTION.Length).Trim();
        }
    }
}