using System;

namespace Lykke.Job.Stellar.Api.Settings
{
    public class StellarJobSettings
    {
        public TimeSpan WalletBalanceJobPeriod { get; set; }

        public TimeSpan TransactionHistoryJobPeriod { get; set; }

        public TimeSpan BroadcastInProgressJobPeriod { get; set; }

        public int BroadcastInProgressJobBatchSize { get; set; }
    }
}