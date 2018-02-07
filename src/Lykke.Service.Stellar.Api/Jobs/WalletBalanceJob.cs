using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Service.Stellar.Api.Jobs
{
    public class WalletBalanceJob : TimerPeriod
    {
        private IBalanceService _balanceService;

        public WalletBalanceJob(IBalanceService balanceService, int period, ILog log)
            : base(nameof(WalletBalanceJob), period, log)
        {
            _balanceService = balanceService;
        }

        public override async Task Execute()
        {
            await _balanceService.UpdateWalletBalances();
        }
    }
}
