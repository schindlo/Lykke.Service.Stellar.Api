using System.Diagnostics;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Service.Stellar.Api.Jobs
{
    public class WalletBalanceJob : TimerPeriod
    {
        private readonly IBalanceService _balanceService;
        private readonly ILog _log;

        public WalletBalanceJob(IBalanceService balanceService, int period, ILog log)
            : base(nameof(WalletBalanceJob), period, log)
        {
            _balanceService = balanceService;
            _log = log;
        }

        public override async Task Execute()
        {
            await _log.WriteInfoAsync(nameof(WalletBalanceJob), nameof(Execute), "Job started");
            var watch = Stopwatch.StartNew();

            try
            {
                int count = await _balanceService.UpdateWalletBalances();

                watch.Stop();
                await _log.WriteInfoAsync(nameof(WalletBalanceJob), nameof(Execute), $"Job finished. dt={watch.ElapsedMilliseconds}ms, records={count}");
            }
            catch (JobExecutionException ex)
            {
                watch.Stop();
                await _log.WriteInfoAsync(nameof(WalletBalanceJob), nameof(Execute), $"Job aborted with exception. dt={watch.ElapsedMilliseconds}ms, records={ex.Processed}");

                throw;
            }
        }
    }
}
