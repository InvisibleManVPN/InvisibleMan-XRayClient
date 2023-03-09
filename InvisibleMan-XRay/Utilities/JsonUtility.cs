using System;
using System.Text.Json;
using Newtonsoft.Json;

namespace InvisibleManXRay.Utilities
{
    public static class JsonUtility
    {
        public static bool IsJsonValid(string json)
        {
            try
            {
                return JsonDocument.Parse(json) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static T ConvertFromJson<T>(string json)
        {
            try
            {
                if (string.IsNullOrEmpty(json))
                    return default;
                    
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default;
            }
        }
    }
}