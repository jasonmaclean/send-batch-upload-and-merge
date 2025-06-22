using Sitecore.Data.Items;
using SitecoreFundamentals.SendBatchUploadAndMerge.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace SitecoreFundamentals.SendBatchUploadAndMerge.Gateways
{
    internal class GatewayUtility
    {
        internal class EndpointFormatter
        {
            private HttpClient HttpClient { get; set; }
            private Item GlobalConfigItem { get; set; }
            private string EndpointField { get; set; }

            private readonly Dictionary<string, string> _tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public EndpointFormatter(HttpClient httpClient, Item globalConfigItem, string endpointField)
            {
                HttpClient = httpClient;
                GlobalConfigItem = globalConfigItem;
                EndpointField = endpointField;
            }

            public string Process()
            {
                if (HttpClient == null || GlobalConfigItem == null || string.IsNullOrWhiteSpace(EndpointField))
                    return "";

                var result = GlobalConfigItem.Fields[EndpointField].Value;

                if (string.IsNullOrWhiteSpace(result))
                    return "";

                AddToken("baseAddress", GlobalConfigItem.Fields[Templates.IntegrationSettings.Send.Fields.BaseAddress].Value);
                AddToken("apiKey", GlobalConfigItem.Fields[Templates.IntegrationSettings.Send.Fields.ApiKey].Value);
                AddToken("format", "json");

                result = _tokens.Aggregate(result, (current, value) =>
                    System.Text.RegularExpressions.Regex.Replace(
                        current,
                        System.Text.RegularExpressions.Regex.Escape(value.Key),
                        value.Value ?? string.Empty,
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    )
                );

                return result;
            }

            public void AddToken(string name, string value)
            {
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(value))
                    return;

                if (!name.StartsWith("{"))
                    name = "{" + name;

                if (!name.EndsWith("{"))
                    name += "}";

                if (_tokens.ContainsKey(name))
                {
                    _tokens[name] = value;
                    return;
                }

                _tokens[name] = value;
            }
        }
    }
}