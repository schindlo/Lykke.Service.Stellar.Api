using System.Diagnostics;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Job.Stellar.Api.Jobs
{
    public class BroadcastInProgressJob : TimerPeriod
    {
        private readonly ITransactionService _transactionService;
        private readonly ILog _log;
        private readonly int _batchSize;

        public BroadcastInProgressJob(ITransactionService transactionService,
                                      ILog log,
                                      int period,
                                      int batchSize)
            : base(nameof(BroadcastInProgressJob), period, log)
        {
            _transactionService = transactionService;
            _log = log;
            _batchSize = batchSize;
        }

        public override async Task Execute()
        {
            await _log.WriteInfoAsync(nameof(BroadcastInProgressJob), nameof(Execute), "Job started");
            var watch = Stopwatch.StartNew();

            try
            {
                int count = await _transactionService.UpdateBroadcastsInProgress(_batchSize);

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
