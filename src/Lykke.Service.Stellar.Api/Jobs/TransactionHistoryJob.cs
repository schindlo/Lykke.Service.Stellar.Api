using System.Diagnostics;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Service.Stellar.Api.Jobs
{
    public class TransactionHistoryJob : TimerPeriod
    {
        private ITransactionHistoryService _txHistoryService;
        private ILog _log;

        public TransactionHistoryJob(ITransactionHistoryService txHistoryService, int period, ILog log)
            : base(nameof(TransactionHistoryJob), period, log)
        {
            _txHistoryService = txHistoryService;
            _log = log;
        }

        public override async Task Execute()
        {
            await _log.WriteInfoAsync(nameof(TransactionHistoryJob), nameof(Execute), $"Job started");
            var watch = Stopwatch.StartNew();

            try 
            {
                int count = await _txHistoryService.UpdateTransactionHistory();

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
