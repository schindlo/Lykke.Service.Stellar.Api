using System.Threading.Tasks;
using StellarSdk.Model;

namespace Lykke.Service.Stellar.Api.Core.Services
{
    public interface IHorizonService
    {
        Task<string> SubmitTransactionAsync(string signedTx);

        Task<TransactionDetails> GetTransactionDetails(string transactionHash);

        Task<Payments> GetPayments(string address, string order = "asc", string cursor = "");

        Task<LedgerDetails> GetLatestLedger();

        Task<AccountDetails> GetAccountDetails(string address);
    }
}
