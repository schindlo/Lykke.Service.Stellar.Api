using System;
using Newtonsoft.Json;
using Lykke.Service.BlockchainApi.Contract.Transactions;

namespace Lykke.Service.Stellar.Api.Models
{
    public class StellarHistoricalTransactionContract: HistoricalTransactionContract
    {
        [JsonProperty("destinationTag")]
        public string DestinationTag { get; set;  }
    }
}
