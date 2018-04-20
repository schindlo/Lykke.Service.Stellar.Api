using System.Collections.Generic;
using System.Threading.Tasks;
using StellarBase.Generated;
using StellarSdk.Model;

namespace Lykke.Service.Stellar.Api.Core.Services
{
    public interface IHorizonService
    {
        Task<string> SubmitTransactionAsync(string signedTx);

        Task<TransactionDetails> GetTransactionDetails(string hash);

        Task<List<TransactionDetails>> GetTransactions(string address, string order = StellarSdkConstants.OrderAsc, string cursor = "");

        Task<Payments> GetPayments(string address, string order = StellarSdkConstants.OrderAsc, string cursor = "");

        Task<LedgerDetails> GetLatestLedger();

        Task<AccountDetails> GetAccountDetails(string address);

        Task<bool> AccountExists(string address);

        Task<long> GetLedgerNoOfLastPayment(string address);

        long GetAccountMergeAmount(string resultXdrBase64, int operationIndex);

        PaymentOp GetFirstPaymentFromTransaction(TransactionDetails tx);

        string GetMemo(TransactionDetails tx);

        string GetTransactionHash(Transaction tx);
    }
}
