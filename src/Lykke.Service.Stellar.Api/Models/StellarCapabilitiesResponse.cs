using System;
using Newtonsoft.Json;

namespace Lykke.Service.Stellar.Api.Models
{
    public class StellarCapabilitiesResponse
    {
        [JsonProperty("areManyInputsSupported")]
        public bool AreManyInputsSupported { get; set; }

        [JsonProperty("areManyOutputsSupported")]
        public bool AreManyOutputsSupported { get; set; }

        [JsonProperty("isTransactionsRebuildingSupported")]
        public bool IsTransactionsRebuildingSupported { get; set; }
    }
}
