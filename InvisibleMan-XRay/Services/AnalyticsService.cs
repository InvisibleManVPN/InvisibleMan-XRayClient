using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace InvisibleManXRay.Services
{
    using Models.Analytics;
    using Models.Analytics.User;
    using Analytics;
    using Utilities;
    using Values;

    public class AnalyticsService : Service
    {
        private Func<string> getClientId;
        private Param[] basicParams;

        public void Setup(Func<string> getClientId, Func<string> getApplicationVersion)
        {
            this.getClientId = getClientId;
            this.basicParams = new[] {
                new Param("engagement_time_msec", "100"),
                new Param("session_id", IdentificationUtility.GenerateSessionId()),
                new Param("AppVersion", getApplicationVersion.Invoke())
            };
        }

        public void SendEvent(IEvent analyticsEvent)
        {
            Param[] eventParams = AppendBasicParams(analyticsEvent.Params);
            UserTier customerTier = new UserTier("standard");

            Payload payload = new Payload() {
                ClientId = getClientId.Invoke(),
                UserProperties = new UserProperties(customerTier),
                Events = new[] {
                    new Event(
                        eventName: $"{analyticsEvent.Scope}_{analyticsEvent.GetType().Name}",
                        eventParams: eventParams
                    )
                }
            };

            SendRequest(payload);
        }

        private Param[] AppendBasicParams(Param[] eventParams)
        {
            return eventParams.Concat(basicParams).ToArray();
        }

        private async void SendRequest(Payload payload)
        {
            StringContent jsonContent = new StringContent(
                content: JsonConvert.SerializeObject(payload),
                encoding: Encoding.UTF8,
                mediaType: "application/json"
            );
            
            HttpClient client = new HttpClient();
            await client.PostAsync(
                requestUri: string.Format(
                    Route.GOOGLE_ANALYTICS, 
                    Security.GA_MEASUREMENT_ID, 
                    Security.GA_API_SECRET
                ),
                content: jsonContent
            );
        }
    }
}