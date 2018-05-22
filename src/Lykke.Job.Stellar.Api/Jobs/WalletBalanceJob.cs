using System.Diagnostics;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Job.Stellar.Api.Jobs
{
    public class WalletBalanceJob : TimerPeriod
    {
        private readonly Stopwatch _watch = Stopwatch.StartNew();
        private readonly IBalanceService _balanceService;
        private readonly ILog _log;

        [UsedImplicitly]
        public WalletBalanceJob(IBalanceService balanceService,
                                int period,
                                ILog log)
            : base(nameof(WalletBalanceJob), period, log)
        {
            _balanceService = balanceService;
            _log = log;
        }

        public override async Task Execute()
        {
            await _log.WriteInfoAsync(nameof(WalletBalanceJob), nameof(Execute), "Job started");
            _watch.Restart();

            try
            {
                var count = await _balanceService.UpdateWalletBalances();

                _watch.Stop();
                await _log.WriteInfoAsync(nameof(WalletBalanceJob), nameof(Execute), $"Job finished. dt={_watch.ElapsedMilliseconds}ms, records={count}");
            }
            catch (JobExecutionException ex)
            {
                _watch.Stop();
                await _log.WriteWarningAsync(nameof(WalletBalanceJob), nameof(Execute), $"Job aborted with exception. dt={_watch.ElapsedMilliseconds}ms, records={ex.Processed}");

                throw;
            }
        }
    }
}
