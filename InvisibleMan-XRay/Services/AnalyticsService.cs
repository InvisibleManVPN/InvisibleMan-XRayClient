using System;
using System.Net;
using System.Text;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        private Func<bool> getSendingAnalyticsEnabled;
        private Param[] basicParams;

        private Queue<Event> bufferedEvents;
        private static readonly object bufferLock = new object();
        
        private const int MAX_EVENTS_PER_BATCH = 25;

        public AnalyticsService()
        {
            this.bufferedEvents = new Queue<Event>();
        }

        public void Setup(
            Func<string> getClientId, 
            Func<bool> getSendingAnalyticsEnabled, 
            Func<string> getApplicationVersion
        )
        {
            this.getClientId = getClientId;
            this.getSendingAnalyticsEnabled = getSendingAnalyticsEnabled;
            this.basicParams = new[] {
                new Param("engagement_time_msec", "100"),
                new Param("session_id", IdentificationUtility.GenerateSessionId()),
                new Param("AppVersion", getApplicationVersion.Invoke())
            };
        }

        public void SendEvent(IEvent analyticsEvent, bool isForced = false)
        {
            if (!IsSendingAnalytics())
                return;

            string eventName = $"{GetAnalyticsEventScope()}_{GetAnalyticsEventName()}";
            Param[] eventParams = AppendBasicParams(analyticsEvent.Params);
            Payload payload = CreatePayload(
                events: new[] { new Event(eventName, eventParams) }
            );

            SendBufferedRequests();
            SendRequest(payload);

            bool IsSendingAnalytics() => getSendingAnalyticsEnabled.Invoke() || isForced;

            string GetAnalyticsEventScope() => analyticsEvent.Scope;

            string GetAnalyticsEventName() => analyticsEvent.GetType().Name.Replace("Event", "");
        }

        private Payload CreatePayload(Event[] events)
        {
            UserTier customerTier = new UserTier("standard");

            return new Payload() {
                ClientId = getClientId.Invoke(),
                UserProperties = new UserProperties(customerTier),
                Events = events
            };
        }

        private Param[] AppendBasicParams(Param[] eventParams)
        {
            return eventParams.Concat(basicParams).ToArray();
        }

        private async void SendRequest(Payload payload)
        {
            string requestUri = string.Format(
                Route.GOOGLE_ANALYTICS,
                Security.GA_MEASUREMENT_ID,
                Security.GA_API_SECRET
            );

            StringContent jsonContent = new StringContent(
                content: JsonConvert.SerializeObject(payload),
                encoding: Encoding.UTF8,
                mediaType: "application/json"
            );
            
            HttpClient client = new HttpClient();
            HttpStatusCode responseCode = await TryPostAsync(requestUri, jsonContent);

            if (!IsRequestSuccess())
                AddToBufferedRequestsList();

            async Task<HttpStatusCode> TryPostAsync(string requestUri, StringContent jsonContent)
            {
                try
                {
                    HttpResponseMessage response = await client.PostAsync(
                        requestUri: requestUri,
                        content: jsonContent
                    );

                    return response.StatusCode;
                }
                catch
                {
                    return HttpStatusCode.NotAcceptable;
                }
            }

            bool IsRequestSuccess()
            {
                return responseCode == System.Net.HttpStatusCode.NoContent;
            }

            void AddToBufferedRequestsList()
            {
                lock (bufferLock)
                {
                    foreach (Event evt in payload.Events)
                        bufferedEvents.Enqueue(evt);
                }
            }
        }

        private void SendBufferedRequests()
        {
            lock (bufferLock)
            {
                Payload payload;
                Event[] events;
                int bufferedEventsBatch;

                while(bufferedEvents.Count > 0)
                {
                    bufferedEventsBatch = Math.Min(bufferedEvents.Count, MAX_EVENTS_PER_BATCH);
                    events = new Event[bufferedEventsBatch];

                    for (int i = 0; i < bufferedEventsBatch; i++)
                        events[i] = bufferedEvents.Dequeue();

                    payload = CreatePayload(events);
                    SendRequest(payload);
                }
            }
        }
    }
}