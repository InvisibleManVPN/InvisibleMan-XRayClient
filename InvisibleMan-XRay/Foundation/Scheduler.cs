using System;
using System.Threading;

namespace InvisibleManXRay.Foundation
{
    public class Scheduler
    {
        public void WaitUntil(Func<bool> condition, int millisecondsTimeout, out bool isConditionSatisfied)
        {
            isConditionSatisfied = false;
            int timeRate = millisecondsTimeout / 20;

            for (int i = 0; i < 20; i++)
            {
                Thread.Sleep(timeRate);

                if (condition.Invoke())
                {
                    isConditionSatisfied = true;
                    break;
                }
            }
        }
    }
}