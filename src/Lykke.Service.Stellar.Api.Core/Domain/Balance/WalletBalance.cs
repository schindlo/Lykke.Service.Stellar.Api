using System;

namespace Lykke.Service.Stellar.Api.Core.Domain.Balance
{
    public class WalletBalance
    {
        public string Address { get; set; }

        public string AssetId { get; set; }

        public string Balance { get; set; }

        public long Block { get; set; }
    }
}
