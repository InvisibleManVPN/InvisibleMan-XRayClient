namespace InvisibleGorillaXRay.Mac.Handlers.DeepLinks
{
    using InvisibleGorillaXRay.Handlers.DeepLinks;

    public class MacDeepLink : IDeepLink
    {
        public void Register()
        {
            // Deep links on macOS require Info.plist CFBundleURLTypes configured at build time.
        }
    }
}
