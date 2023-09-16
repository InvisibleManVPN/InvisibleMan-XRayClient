using System;
using System.Text;
using System.Collections.Generic;

namespace InvisibleManXRay.Models.Templates.Subscriptions
{
    public abstract class Template
    {
        protected string Data;

        public abstract bool IsValid(string link);

        public abstract Status FetchDataFromLink(string link);

        public List<string[]> ConvertToV2RayList(Func<string, Status> convertConfigLinkToV2Ray)
        {
            string data;
            List<string[]> v2RayList = new List<string[]>();

            TryDecode();
            TryConvert();
            return v2RayList;

            void TryDecode()
            {
                TryDecodeAsBase64();
                if (IsDecodeSucceeded())
                    return;
                
                TryDecodeAsStringArray();
                if (IsDecodeSucceeded())
                    return;

                void TryDecodeAsBase64()
                {
                    try
                    {
                        data = Encoding.UTF8.GetString(
                            bytes: System.Convert.FromBase64String(Data)
                        );
                    }
                    catch
                    {
                        data = null;
                    }
                }

                void TryDecodeAsStringArray()
                {
                    data = Data;
                }

                bool IsDecodeSucceeded() => !string.IsNullOrEmpty(data);
            }

            void TryConvert()
            {
                foreach(string link in data.Split("\n"))
                {
                    Status convertingStatus = convertConfigLinkToV2Ray.Invoke(link);
                    if (convertingStatus.Code == Code.SUCCESS)
                    {
                        string[] config = GetConfig(convertingStatus);
                        v2RayList.Add(
                            new[] { GetConfigRemark(config), GetConfigData(config) }
                        );
                    }
                }

                string[] GetConfig(Status configStatus) => (string[])configStatus.Content;

                string GetConfigRemark(string[] config) => config[0];

                string GetConfigData(string[] config) => config[1];
            }
        }

        protected bool IsAnyDataExisits() => !string.IsNullOrEmpty(Data);
    }
}