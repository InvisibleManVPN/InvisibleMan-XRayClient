using System;
using System.Collections.Generic;

namespace InvisibleGorillaXRay.Services
{
    public class LocalizationService : Service
    {
        private Func<string, string> getTermFunc;

        public void Setup(Func<string, string> getTermFunc)
        {
            this.getTermFunc = getTermFunc;
        }

        public string GetTerm(string key)
        {
            return getTermFunc?.Invoke(key) ?? key;
        }
    }
}
