using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Service.Stellar.Api.Jobs
{
    public class BroadcastInProgressJob : TimerPeriod
    {
        private ITransactionService _transactionService;

        public BroadcastInProgressJob(ITransactionService transactionService, int period, ILog log)
            : base(nameof(BroadcastInProgressJob), period, log)
        {
            _transactionService = transactionService;
        }

        public override async Task Execute()
        {
            await _transactionService.UpdateBroadcastsInProgress();
        }
    }
}
