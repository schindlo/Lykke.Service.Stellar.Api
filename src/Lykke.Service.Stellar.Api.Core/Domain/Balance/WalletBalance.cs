﻿namespace Lykke.Service.Stellar.Api.Core.Domain.Balance
{
    public class WalletBalance
    {
        public string AssetId { get; set; }

        public string Address { get; set; }

        public string DestinationTag { get; set; }

        public long Balance { get; set; }

        public long Ledger { get; set; }
    }
}