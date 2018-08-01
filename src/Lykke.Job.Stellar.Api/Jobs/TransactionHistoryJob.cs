using System.Diagnostics;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
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
                                     ILog log,
                                     int period)
            : base(nameof(TransactionHistoryJob), period, log)
        {
            _txHistoryService = txHistoryService;
            _log = log;
        }

        public override async Task Execute()
        {
            await _log.WriteInfoAsync(nameof(TransactionHistoryJob), nameof(Execute), "Job started");
            _watch.Restart();

            try 
            {
                var count = await _txHistoryService.UpdateDepositBaseTransactionHistory();

                _watch.Stop();
                await _log.WriteInfoAsync(nameof(TransactionHistoryJob), nameof(Execute), $"Job finished. dt={_watch.ElapsedMilliseconds}ms, records={count}");
            }
            catch (JobExecutionException ex)
            {
                _watch.Stop();
                await _log.WriteWarningAsync(nameof(TransactionHistoryJob), nameof(Execute), $"Job aborted with exception. dt={_watch.ElapsedMilliseconds}ms, records={ex.Processed}");

                throw;
            }
        }
    }
}
