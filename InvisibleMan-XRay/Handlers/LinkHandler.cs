using System.Diagnostics;

namespace InvisibleManXRay.Handlers
{
    using Values;

    public class LinkHandler : Handler
    {
        public void OpenWebsiteLink() => OpenLink(Route.WEBSITE);

        public void OpenEmailLink() => OpenLink(Route.EMAIL);

        public void OpenGitHubRepositoryLink() => OpenLink(Route.REPOSITORY);

        public void OpenBugReportingLink() => OpenLink(Route.ISSUES);

        public void OpenLatestReleaseLink() => OpenLink(Route.LATEST_RELEASE);

        public void OpenCustomLink(string link) => OpenLink(link);

        private void OpenLink(string link)
        {
            Process.Start(new ProcessStartInfo(link) {
                UseShellExecute = true
            });
        }
    }
}