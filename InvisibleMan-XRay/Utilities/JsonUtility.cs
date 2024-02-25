using System;
using System.Text.Json;

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
    }
}