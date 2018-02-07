using System;
using Lykke.Service.BlockchainApi.Contract.Transactions;

namespace Lykke.Service.Stellar.Api.Models
{
    public class StellarHistoricalTransactionContract: HistoricalTransactionContract
    {
        public string DestinationTag { get; set;  }
    }
}
