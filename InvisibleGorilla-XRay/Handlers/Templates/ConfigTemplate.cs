using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace InvisibleGorillaXRay.Handlers.Templates
{
    using Services;
    using Models;
    using Models.Templates.Configs;
    using Utilities;
    using Values;

    public class ConfigTemplate : ITemplate
    {
        private Dictionary<string, Type> templates;

        private LocalizationService LocalizationService => ServiceLocator.Get<LocalizationService>();

        public ConfigTemplate()
        {
            this.templates = new Dictionary<string, Type>();
        }

        public void RegisterTemplates()
        {
            templates.Add("vmess", typeof(Vmess));
            templates.Add("vless", typeof(Vless));
            templates.Add("trojan", typeof(Trojan));
            templates.Add("ss", typeof(Shadowsocks));
        }

        public Status ConverLinkToConfig(string link)
        {
            if (TryConvertDataConfigLink(link, out Status dataConfigStatus))
                return dataConfigStatus;

            Template template = FindTemplate(type: FetchConfigType());
            if (template == null)
                return new Status(
                    code: Code.ERROR,
                    subCode: SubCode.UNSUPPORTED_LINK,
                    content: LocalizationService.GetTerm(Localization.UNSUPPORTED_CONFIG_LINK)
                );

            Status fetchingStatus = template.FetchDataFromLink(link);
            if (fetchingStatus.Code == Code.ERROR)
                return fetchingStatus;

            V2Ray v2Ray = template.ConvertToV2Ray();
            string remark = template.GetValidRemark();

            return new Status(
                code: Code.SUCCESS,
                subCode: SubCode.SUCCESS,
                content: new string[] { remark, JsonConvert.SerializeObject(v2Ray) }
            );

            string FetchConfigType() => link.Split("://").First();

            bool TryConvertDataConfigLink(string value, out Status status)
            {
                status = null;
                value = NormalizeDataConfigLink(value);

                if (string.IsNullOrWhiteSpace(value) ||
                    !value.StartsWith("data:application/json;", StringComparison.OrdinalIgnoreCase))
                    return false;

                status = ConvertDataConfigLink(value);
                return true;
            }

            string NormalizeDataConfigLink(string value)
            {
                string normalized = value?.Trim() ?? string.Empty;
                if (normalized.StartsWith(DeepLink.CONFIG_DATA, StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(normalized.Substring(DeepLink.CONFIG_DATA.Length).Trim());

                return normalized;
            }

            Status ConvertDataConfigLink(string value)
            {
                try
                {
                    int separatorIndex = value.IndexOf(',');
                    if (separatorIndex <= 0)
                        throw new FormatException();

                    string metadata = value.Substring(0, separatorIndex);
                    string payload = value.Substring(separatorIndex + 1);
                    if (!metadata.Split(';').Any(part => part.Equals("base64", StringComparison.OrdinalIgnoreCase)))
                        throw new FormatException();

                    string configJson = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
                    if (!JsonUtility.IsJsonValid(configJson))
                        return new Status(
                            code: Code.ERROR,
                            subCode: SubCode.INVALID_CONFIG,
                            content: LocalizationService.GetTerm(Localization.INVALID_CONFIG)
                        );

                    return new Status(
                        code: Code.SUCCESS,
                        subCode: SubCode.SUCCESS,
                        content: new string[] { GetDataConfigRemark(metadata), configJson }
                    );
                }
                catch
                {
                    return new Status(
                        code: Code.ERROR,
                        subCode: SubCode.INVALID_CONFIG,
                        content: LocalizationService.GetTerm(Localization.INVALID_CONFIG)
                    );
                }
            }

            string GetDataConfigRemark(string metadata)
            {
                string name = metadata
                    .Split(';')
                    .FirstOrDefault(part => part.StartsWith("name=", StringComparison.OrdinalIgnoreCase))
                    ?.Substring("name=".Length);

                name = string.IsNullOrWhiteSpace(name)
                    ? "config"
                    : Uri.UnescapeDataString(name);

                name = System.IO.Path.GetFileNameWithoutExtension(name);
                return FileUtility.GetValidFileName(string.IsNullOrWhiteSpace(name) ? "config" : name);
            }

            Template FindTemplate(string type)
            {
                var template = templates.FirstOrDefault(
                    (element) => element.Key == type.ToLower()
                );

                if (template.Key == null)
                    return null;
                    
                return Activator.CreateInstance(template.Value) as Template;
            }
        }
    }
}