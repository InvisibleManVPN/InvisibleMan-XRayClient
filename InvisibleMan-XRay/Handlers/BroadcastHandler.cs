using System;
using System.Net;
using Newtonsoft.Json;

namespace InvisibleManXRay.Handlers
{
    using Models;
    using Values;

    public class BroadcastHandler : Handler
    {
        public Status CheckForBroadcast()
        {
            Broadcast broadcast = GetBroadcast();
            if (IsBroadcastAvailable())
                return new Status(Code.SUCCESS, SubCode.SUCCESS, broadcast);
            else
                return new Status(Code.ERROR, SubCode.BROADCAST_UNAVAILABLE, null);

            Broadcast GetBroadcast()
            {
                try
                {
                    WebClient webClient = new WebClient();
                    string rawData = webClient.DownloadString(Route.BROADCAST);
                    return JsonConvert.DeserializeObject<Broadcast>(rawData);
                }
                catch(Exception)
                {
                    return null;
                }
            }

            bool IsBroadcastAvailable() => broadcast != null && !string.IsNullOrEmpty(broadcast.Text);
        }
    }
}