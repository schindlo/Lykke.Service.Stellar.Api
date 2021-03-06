﻿namespace Lykke.Service.Stellar.Api.Core.Settings.ServiceSettings
{
    public class StellarApiSettings
    {
        public DbSettings Db { get; set; }

        public string HorizonUrl { get; set; }

        public int WalletBalanceJobPeriodSeconds { get; set; }

        public int TransactionHistoryJobPeriodSeconds { get; set; }

        public int BroadcastInProgressJobPeriodSeconds { get; set; }

        public int WalletBalanceJobBatchSize { get; set; }

        public int TransactionHistoryJobBatchSize { get; set; }

        public int BroadcastInProgressJobBatchSize { get; set; }
    }
}
