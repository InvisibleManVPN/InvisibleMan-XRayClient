using System;
using System.Linq;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        public static string Find(string key, string parent, string jsonString)
        {
            return JObject.Parse(jsonString.ToLower())
                .DescendantsAndSelf().OfType<JProperty>()
                .Single(x => x.Name.Equals(parent) && x.SelectToken($"$..{key}") != null)
                .SelectToken($"$..{key}").ToString();
        }
    }
}