using System.Diagnostics;

namespace InvisibleManXRay.Handlers
{
    using Values;

    public class LinkHandler : Handler
    {
        public void OpenLatestReleaseLink()
        {
            Process.Start(new ProcessStartInfo(Route.LATEST_RELEASE) {
                UseShellExecute = true
            });
        }
    }
}