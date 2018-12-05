extern alias sdk2;
using System.Collections.Generic;
using System.Threading.Tasks;
using sdk2::stellar_dotnet_sdk.responses.operations;
using StellarBase.Generated;
using StellarSdk.Model;
using static StellarBase.Generated.Operation;

namespace Lykke.Service.Stellar.Api.Core.Services
{
    public interface IHorizonService
    {
        Task<string> SubmitTransactionAsync(string signedTx);

        Task<TransactionDetails> GetTransactionDetails(string hash);

        /// <param name="address"></param>
        /// <param name="order"></param>
        /// <param name="cursor"></param>
        /// <param name="limit">200 is max</param>
        /// <returns></returns>
        Task<List<TransactionDetails>> GetTransactions(string address, string order = StellarSdkConstants.OrderAsc, string cursor = "", int limit = 100);

        Task<List<OperationResponse>> GetTransactionOperations(string hash);

        Task<LedgerDetails> GetLatestLedger();

        Task<AccountDetails> GetAccountDetails(string address);

        Task<bool> AccountExists(string address);

        long GetAccountMergeAmount(string resultXdrBase64, int operationIndex);

        long GetAccountMergeAmount(string metaXdrBase64, string sourceAddress);

        OperationBody GetFirstOperationFromTxEnvelopeXdr(string xdrBase64);

        OperationBody GetFirstOperationFromTxEnvelope(TransactionEnvelope txEnvelope);

        string GetMemo(TransactionDetails tx);

        string GetTransactionHash(Transaction tx);
    }
}
