using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Service.Stellar.Api.Jobs
{
    public class TransactionHistoryJob : TimerPeriod
    {
        private ITransactionHistoryService _txHistoryService;

        public TransactionHistoryJob(ITransactionHistoryService txHistoryService, int period, ILog log)
            : base(nameof(TransactionHistoryJob), period, log)
        {
            _txHistoryService = txHistoryService;
        }

        public override async Task Execute()
        {
            await _txHistoryService.UpdateTransactionHistory();
        }
    }
}
