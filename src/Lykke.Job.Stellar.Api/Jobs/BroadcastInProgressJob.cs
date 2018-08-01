using System.Diagnostics;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Job.Stellar.Api.Jobs
{
    public class BroadcastInProgressJob : TimerPeriod
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private readonly ITransactionService _transactionService;
        private readonly ILog _log;
        private readonly int _batchSize;

        [UsedImplicitly]
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
            _watch.Restart();

            try
            {
                var count = await _transactionService.UpdateBroadcastsInProgress(_batchSize);

                _watch.Stop();
                await _log.WriteInfoAsync(nameof(BroadcastInProgressJob), nameof(Execute), $"Job finished. dt={_watch.ElapsedMilliseconds}ms, records={count}");
            }
            catch (JobExecutionException ex)
            {
                _watch.Stop();
                await _log.WriteWarningAsync(nameof(BroadcastInProgressJob), nameof(Execute), $"Job aborted with exception. dt={_watch.ElapsedMilliseconds}ms, records={ex.Processed}");

                throw;
            }
        }
    }
}
