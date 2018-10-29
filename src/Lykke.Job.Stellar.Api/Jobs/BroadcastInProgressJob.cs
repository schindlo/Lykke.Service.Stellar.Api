using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
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
                                      ILogFactory logFactory,
                                      TimeSpan period,
                                      int batchSize)
            : base(period, logFactory)
        {
            _transactionService = transactionService;
            _log = logFactory.CreateLog(this);
            _batchSize = batchSize;
        }

        public override async Task Execute()
        {
            _log.Debug("Job started");
            _watch.Restart();

            try
            {
                var count = await _transactionService.UpdateBroadcastsInProgress(_batchSize);

                _watch.Stop();
                _log.Debug($"Job finished. dt={_watch.ElapsedMilliseconds}ms, records={count}");
            }
            catch (JobExecutionException ex)
            {
                _watch.Stop();
                _log.Warning($"Job aborted with exception. dt={_watch.ElapsedMilliseconds}ms, records={ex.Processed}");

                throw;
            }
        }
    }
}
