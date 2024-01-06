using System;
using System.Threading;

namespace InvisibleManXRay.Foundation
{
    public class Scheduler
    {
        public void WaitUntil(
            Func<bool> condition, 
            Func<bool> cancellation, 
            out bool isConditionSatisfied
        )
        {
            isConditionSatisfied = false;

            while(true)
            {
                Thread.Sleep(100);

                if (cancellation.Invoke())
                    break;

                if (condition.Invoke())
                {
                    isConditionSatisfied = true;
                    break;
                }
            }
        }
    }
}