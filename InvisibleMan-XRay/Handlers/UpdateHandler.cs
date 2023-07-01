using System;
using System.Net;
using System.Linq;

namespace InvisibleManXRay.Handlers
{
    using Models;
    using Values;

    public class UpdateHandler : Handler
    {
        private Func<AppVersion> getApplicationVersion;
        private Func<string, AppVersion> convertToAppVersion;

        public void Setup(
            Func<AppVersion> getApplicationVersion,
            Func<string, AppVersion> convertToAppVersion
        )
        {
            this.getApplicationVersion = getApplicationVersion;
            this.convertToAppVersion = convertToAppVersion;
        }

        public Status CheckForUpdate()
        {
            string latestReleaseUrl = GetLatestReleaseUrl();
            string latestReleaseVersion = GetLatestReleaseVersion(latestReleaseUrl);
            if (latestReleaseVersion == null)
                return new Status(Code.ERROR, SubCode.CANT_CONNECT, Message.CANT_CONNECT_TO_SERVER);

            if (IsUpdateAvailable())
                return new Status(Code.SUCCESS, SubCode.UPDATE_AVAILABLE, Message.UPDATE_AVAILABLE);
            
            return new Status(Code.SUCCESS, SubCode.UPDATE_UNAVAILABLE, Message.YOU_HAVE_LATEST_VERSION);

            bool IsUpdateAvailable()
            {
                AppVersion latestReleaseAppVersion = convertToAppVersion.Invoke(latestReleaseVersion);
                AppVersion currentReleaseAppVersion = getApplicationVersion.Invoke();
                
                if (latestReleaseAppVersion.Major > currentReleaseAppVersion.Major)
                    return true;
                else if (latestReleaseAppVersion.Major < currentReleaseAppVersion.Major)
                    return false;
                
                if (latestReleaseAppVersion.Feature > currentReleaseAppVersion.Feature)
                    return true;
                else if (latestReleaseAppVersion.Feature < currentReleaseAppVersion.Feature)
                    return false;
                
                if (latestReleaseAppVersion.BugFix > currentReleaseAppVersion.BugFix)
                    return true;
                else if (latestReleaseAppVersion.BugFix < currentReleaseAppVersion.BugFix)
                    return false;
                
                return false;
            }
        }

        private string GetLatestReleaseUrl()
        {
            try
            {
                HttpWebRequest request = HttpWebRequest.CreateHttp(Route.LATEST_RELEASE) as HttpWebRequest;
                request.Method = "GET";
                request.AllowAutoRedirect = false;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                
                return response.Headers["Location"];
            }
            catch(Exception)
            {
                return null;
            }
        }

        private string GetLatestReleaseVersion(string latestReleaseUrl)
        {
            return latestReleaseUrl == null ? null : latestReleaseUrl.Split("/").Last().Replace("v", "");
        }
    }
}