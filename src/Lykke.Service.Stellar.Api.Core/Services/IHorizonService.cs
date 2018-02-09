using System.Threading.Tasks;
using StellarBase.Generated;
using StellarSdk.Model;

namespace Lykke.Service.Stellar.Api.Core.Services
{
    public interface IHorizonService
    {
        Task<string> SubmitTransactionAsync(string signedTx);

        Task<TransactionDetails> GetTransactionDetails(string hash);

        Task<Payments> GetPayments(string address, string order = StellarSdkConstants.OrderAsc, string cursor = "");

        Task<LedgerDetails> GetLatestLedger();

        Task<AccountDetails> GetAccountDetails(string address);

        Task<long> GetLedgerNoOfLastPayment(string address);

        long GetAccountMergeAmount(string resultXdrBase64, int accountMergeInTx);

        PaymentOp GetFirstPaymentFromTransaction(TransactionDetails tx);
    }
}
