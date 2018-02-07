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
        private ITransactionHistoryService _txHistoryService;

        public TransactionHistoryJob(ITransactionHistoryService txHistoryService, int period, ILog log)
            : base(nameof(TransactionHistoryJob), period, log)
        {
            _log = log;
            _txHistoryService = txHistoryService;
        }

        public override async Task Execute()
        {
            try
            {
                await _txHistoryService.UpdateTransactionHistory();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(TransactionHistoryJob), nameof(Execute),
                    "Failed to execute transaction history update", ex);
            }
        }
    }
}
