using System;

namespace InvisibleManXRay.Handlers
{
    using Models;

    public class VersionHandler : Handler
    {
        public AppVersion ConvertToAppVersion(string version)
        {
            string[] versionElements = version.Split('.');
                
            return new AppVersion() {
                Major = versionElements.Length > 0 ? TryConvertStringToInt(versionElements[0]) : 0,
                Feature = versionElements.Length > 1 ? TryConvertStringToInt(versionElements[1]) : 0,
                BugFix = versionElements.Length > 2 ? TryConvertStringToInt(versionElements[2]) : 0
            };

            int TryConvertStringToInt(string str)
            {
                try
                {
                    return int.Parse(str);
                }
                catch(Exception)
                {
                    return 0;
                }
            }
        }

        public AppVersion GetApplicationVersion()
        {
            Version version = GetType().Assembly.GetName().Version;
            return ConvertToAppVersion($"{version.Major}.{version.Minor}.{version.Build}");
        }
    }
}