using System;
using System.Threading;

namespace InvisibleGorillaXRay.Foundation
{
    public class Scheduler
    {
        private const int PollIntervalMs = 100;

        public bool WaitUntil(
            Func<bool> condition, 
            Func<bool> cancellation, 
            int timeoutMs,
            out bool isConditionSatisfied
        )
        {
            isConditionSatisfied = false;
            int elapsedMs = 0;

            while (elapsedMs < timeoutMs)
            {
                if (cancellation.Invoke())
                    return false;

                if (condition.Invoke())
                {
                    isConditionSatisfied = true;
                    return false;
                }

                Thread.Sleep(PollIntervalMs);
                elapsedMs += PollIntervalMs;
            }

            return true;
        }
    }
}