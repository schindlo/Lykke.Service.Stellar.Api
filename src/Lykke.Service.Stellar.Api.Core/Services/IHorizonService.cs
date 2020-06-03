using System.Collections.Generic;
using System.Threading.Tasks;
using stellar_dotnet_sdk.requests;
using stellar_dotnet_sdk.responses;
using stellar_dotnet_sdk.xdr;
using OperationResponse = stellar_dotnet_sdk.responses.operations.OperationResponse;

namespace Lykke.Service.Stellar.Api.Core.Services
{
    public interface IHorizonService
    {
        Task<string> SubmitTransactionAsync(string signedTx);

        Task<TransactionResponse> GetTransactionDetails(string hash);

        /// <param name="address"></param>
        /// <param name="order"></param>
        /// <param name="cursor"></param>
        /// <param name="limit">200 is max</param>
        /// <returns></returns>
        Task<List<TransactionResponse>> GetTransactions(string address,
            OrderDirection order = OrderDirection.ASC, string cursor = "", int limit = 100);

        Task<List<OperationResponse>> GetTransactionOperations(string hash);

        Task<LedgerResponse> GetLatestLedger();

        Task<AccountResponse> GetAccountDetails(string address);

        Task<bool> AccountExists(string address);

        long GetAccountMergeAmount(string resultXdrBase64, int operationIndex);

        long GetAccountMergeAmount(string metaXdrBase64, string sourceAddress);

        Operation.OperationBody GetFirstOperationFromTxEnvelopeXdr(string xdrBase64);

        Operation.OperationBody GetFirstOperationFromTxEnvelope(TransactionEnvelope txEnvelope);

        string GetMemo(TransactionResponse tx);

        string GetTransactionHash(Transaction tx);
    }
}
