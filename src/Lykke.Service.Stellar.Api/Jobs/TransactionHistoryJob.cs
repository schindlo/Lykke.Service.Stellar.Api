using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Service.Stellar.Api.Jobs
{
    public class TransactionHistoryJob : TimerPeriod
    {
        private ILog _log;
        private ITransactionHistoryService _transactionService;

        public TransactionHistoryJob(ITransactionHistoryService transactionService, int period, ILog log)
            : base(nameof(TransactionHistoryJob), period, log)
        {
            _log = log;
            _transactionService = transactionService;
        }

        public override async Task Execute()
        {
            try
            {
                await _transactionService.UpdateTransactionHistory();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(TransactionHistoryJob), nameof(Execute),
                    "Failed to execute transaction history update", ex);
            }
        }
    }
}
