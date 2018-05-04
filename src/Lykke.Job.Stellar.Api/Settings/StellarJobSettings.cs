namespace Lykke.Job.Stellar.Api.Settings
{
    public class StellarJobSettings
    {
        public int WalletBalanceJobPeriodSeconds { get; set; }

        public int TransactionHistoryJobPeriodSeconds { get; set; }

        public int BroadcastInProgressJobPeriodSeconds { get; set; }

        public int BroadcastInProgressJobBatchSize { get; set; }
    }
}