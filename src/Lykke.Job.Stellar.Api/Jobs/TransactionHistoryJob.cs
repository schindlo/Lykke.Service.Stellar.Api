using System.Diagnostics;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Job.Stellar.Api.Jobs
{
    public class TransactionHistoryJob : TimerPeriod
    {
        private readonly ITransactionHistoryService _txHistoryService;
        private readonly ILog _log;
        private readonly int _batchSize;

        public TransactionHistoryJob(ITransactionHistoryService txHistoryService, ILog log, int period, int batchSize)
            : base(nameof(TransactionHistoryJob), period, log)
        {
            _txHistoryService = txHistoryService;
            _log = log;
            _batchSize = batchSize;
        }

        public override async Task Execute()
        {
            await _log.WriteInfoAsync(nameof(TransactionHistoryJob), nameof(Execute), "Job started");
            var watch = Stopwatch.StartNew();

            try 
            {
                int count = await _txHistoryService.UpdateTransactionHistory(_batchSize);

                watch.Stop();
                await _log.WriteInfoAsync(nameof(TransactionHistoryJob), nameof(Execute), $"Job finished. dt={watch.ElapsedMilliseconds}ms, records={count}");
            }
            catch (JobExecutionException ex)
            {
                watch.Stop();
                await _log.WriteInfoAsync(nameof(TransactionHistoryJob), nameof(Execute), $"Job aborted with exception. dt={watch.ElapsedMilliseconds}ms, records={ex.Processed}");

                throw;
            }
        }
    }
}
