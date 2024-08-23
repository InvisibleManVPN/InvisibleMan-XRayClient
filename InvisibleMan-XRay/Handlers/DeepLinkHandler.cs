namespace InvisibleManXRay.Handlers
{
    using DeepLinks;

    public class DeepLinkHandler : Handler
    {
        private IDeepLink deepLink;

        public DeepLinkHandler()
        {
            InitializeDeepLink();
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
    }
}