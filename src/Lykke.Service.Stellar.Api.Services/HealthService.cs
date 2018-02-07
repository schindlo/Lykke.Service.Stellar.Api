using System.Collections.Generic;
using Lykke.Service.Stellar.Api.Core.Domain.Health;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Service.Stellar.Api.Services
{
    // NOTE: See https://lykkex.atlassian.net/wiki/spaces/LKEWALLET/pages/35755585/Add+your+app+to+Monitoring
    public class HealthService : IHealthService
    {
        private IBalanceService _balanceService;
        private ITransactionHistoryService _txHistoryService;

        public HealthService(IBalanceService balanceService, ITransactionHistoryService txHistoryService)
        {
            _balanceService = balanceService;
            _txHistoryService = txHistoryService;
        }
                                
        public string GetHealthViolationMessage()
        {
            List<string> issues = new List<string>();
            if(_balanceService.GetLastJobError() != null)
            {
                issues.Add(_balanceService.GetLastJobError());
            }
            if(_txHistoryService.GetLastJobError() != null)
            {
                issues.Add(_txHistoryService.GetLastJobError());
            }
            if(issues.Count > 0)
            {
                return string.Join(",\n", issues.ToArray());
            }

            return null;
        }

        public IEnumerable<HealthIssue> GetHealthIssues()
        {
            var issues = new HealthIssuesCollection();

            // TODO: Check gathered health statistics, and add appropriate health issues message to issues

            return issues;
        }
    }
}