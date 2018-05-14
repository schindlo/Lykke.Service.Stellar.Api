using System.Collections.Generic;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Common.Health;

namespace Lykke.Service.Stellar.Api.Services
{
    // NOTE: See https://lykkex.atlassian.net/wiki/spaces/LKEWALLET/pages/35755585/Add+your+app+to+Monitoring
    public class HealthService : IHealthService
    {
        private const string JobExecutionIssueType = "JobExecution";

        private readonly IBalanceService _balanceService;
        private readonly ITransactionHistoryService _txHistoryService;
        private readonly ITransactionService _transactionService;

        public HealthService(IBalanceService balanceService,
                             ITransactionHistoryService txHistoryService,
                             ITransactionService transactionService)
        {
            _balanceService = balanceService;
            _txHistoryService = txHistoryService;
            _transactionService = transactionService;
        }

        public string GetHealthViolationMessage()
        {
            return null;
        }

        public IEnumerable<HealthIssue> GetHealthIssues()
        {
            var issues = new HealthIssuesCollection();

            if (_balanceService.GetLastJobError() != null)
            {
                issues.Add(JobExecutionIssueType, _balanceService.GetLastJobError());
            }
            if (_txHistoryService.GetLastJobError() != null)
            {
                issues.Add(JobExecutionIssueType, _txHistoryService.GetLastJobError());
            }
            if (_transactionService.GetLastJobError() != null)
            {
                issues.Add(JobExecutionIssueType, _transactionService.GetLastJobError());
            }

            return issues;
        }
    }
}