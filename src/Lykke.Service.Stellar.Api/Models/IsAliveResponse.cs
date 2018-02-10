using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lykke.Service.Stellar.Api.Models
{
    public class IsAliveResponse
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("env")]
        public string Env { get; set; }

        [JsonProperty("isDebug")]
        public bool IsDebug { get; set; }

        [JsonProperty("issueIndicators")]
        public IEnumerable<IssueIndicator> IssueIndicators { get; set; }

        public class IssueIndicator
        {
            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }
        }
    }
}
