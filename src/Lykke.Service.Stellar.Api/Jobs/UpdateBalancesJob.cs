using System;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Service.Stellar.Api.Jobs
{
    public class UpdateBalancesJob : TimerPeriod
    {
        private ILog _log;
        private IBalanceService _balanceService;

        public UpdateBalancesJob(IBalanceService balanceService, int period, ILog log)
            : base(nameof(UpdateBalancesJob), period, log)
        {
            _log = log;
            _balanceService = balanceService;
        }

        public override async Task Execute()
        {
            try
            {
                await _balanceService.UpdateBalances();
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(UpdateBalancesJob), nameof(Execute),
                    "Failed to execute balances update", ex);
            }
        }
    }
}
