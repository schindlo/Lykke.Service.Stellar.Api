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
    public class TransactionHistoryJob : TimerPeriod
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private readonly ITransactionHistoryService _txHistoryService;
        private readonly ILog _log;

        [UsedImplicitly]
        public TransactionHistoryJob(ITransactionHistoryService txHistoryService,
                                     ILogFactory logFactory,
                                     TimeSpan period)
            : base(period, logFactory)
        {
            _txHistoryService = txHistoryService;
            _log = logFactory.CreateLog(this);
        }

        public override async Task Execute()
        {
            _log.Info("Job started");
            _watch.Restart();

            try 
            {
                var count = await _txHistoryService.UpdateDepositBaseTransactionHistory();

                _watch.Stop();
                _log.Info($"Job finished. dt={_watch.ElapsedMilliseconds}ms, records={count}");
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
