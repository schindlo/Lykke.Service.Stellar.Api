using System.Diagnostics;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Service.Stellar.Api.Jobs
{
    public class WalletBalanceJob : TimerPeriod
    {
        private IBalanceService _balanceService;
        private ILog _log;

        public WalletBalanceJob(IBalanceService balanceService, int period, ILog log)
            : base(nameof(WalletBalanceJob), period, log)
        {
            _balanceService = balanceService;
            _log = log;
        }

        public override async Task Execute()
        {
            await _log.WriteMonitorAsync(nameof(WalletBalanceJob), nameof(Execute), $"Job started");
            var watch = Stopwatch.StartNew();

            int count = await _balanceService.UpdateWalletBalances();

            watch.Stop();
            await _log.WriteMonitorAsync(nameof(WalletBalanceJob), nameof(Execute), $"Job finished. dt={watch.ElapsedMilliseconds}ms, records={count}");
        }
    }
}
