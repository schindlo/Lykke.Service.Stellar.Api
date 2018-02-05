using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Service.Stellar.Api.Jobs
{
    public class WalletBalanceJob : TimerPeriod
    {
        private ILog _log;
        private IBalanceService _balanceService;

        public WalletBalanceJob(IBalanceService balanceService, int period, ILog log)
            : base(nameof(WalletBalanceJob), period, log)
        {
            _log = log;
            _balanceService = balanceService;
        }

        public override async Task Execute()
        {
            try
            {
                await _balanceService.UpdateWalletBalances();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(WalletBalanceJob), nameof(Execute),
                    "Failed to execute balances update", ex);
            }
        }
    }
}
