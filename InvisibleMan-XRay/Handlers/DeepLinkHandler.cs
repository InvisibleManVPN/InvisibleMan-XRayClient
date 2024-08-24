using System;

namespace InvisibleManXRay.Handlers
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
            if (IsValidConfigLink())
                onConfigLinkFetched.Invoke(GetConfigLink());
            else if (IsValidSubscriptionLink())
                onSubscriptionLinkFetched.Invoke(GetSubscriptionLink());

            bool IsValidConfigLink()
            {
                return arg.StartsWith(DeepLink.CONFIG)
                    && !string.IsNullOrEmpty(GetConfigLink());
            }

            bool IsValidSubscriptionLink()
            {
                return arg.StartsWith(DeepLink.SUBSCRIPTION)
                    && !string.IsNullOrEmpty(GetSubscriptionLink());
            }

            string GetConfigLink() => arg.Replace(DeepLink.CONFIG, "").Trim();

            string GetSubscriptionLink() => arg.Replace(DeepLink.SUBSCRIPTION, "").Trim();
        }
    }
}