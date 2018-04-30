using System.Diagnostics;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Service.Stellar.Api.Jobs
{
    public class BroadcastInProgressJob : TimerPeriod
    {
        private readonly ITransactionService _transactionService;
        private readonly ILog _log;

        public BroadcastInProgressJob(ITransactionService transactionService, int period, ILog log)
            : base(nameof(BroadcastInProgressJob), period, log)
        {
            _transactionService = transactionService;
            _log = log;
        }

        public override async Task Execute()
        {
            await _log.WriteInfoAsync(nameof(BroadcastInProgressJob), nameof(Execute), "Job started");
            var watch = Stopwatch.StartNew();

            try
            {
                int count = await _transactionService.UpdateBroadcastsInProgress();

                watch.Stop();
                await _log.WriteInfoAsync(nameof(BroadcastInProgressJob), nameof(Execute), $"Job finished. dt={watch.ElapsedMilliseconds}ms, records={count}");
            }
            catch (JobExecutionException ex)
            {
                watch.Stop();
                await _log.WriteInfoAsync(nameof(BroadcastInProgressJob), nameof(Execute), $"Job aborted with exception. dt={watch.ElapsedMilliseconds}ms, records={ex.Processed}");

                throw;
            }
        }
    }
}
