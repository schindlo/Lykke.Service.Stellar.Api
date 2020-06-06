using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Chaos.NaCl;
using JetBrains.Annotations;
using Lykke.Service.Stellar.Api.Core;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Settings;
using stellar_dotnet_sdk;
using stellar_dotnet_sdk.federation;
using stellar_dotnet_sdk.requests;
using stellar_dotnet_sdk.responses;
using stellar_dotnet_sdk.responses.operations;
using stellar_dotnet_sdk.xdr;
using Operation = stellar_dotnet_sdk.xdr.Operation;
using TransactionResult = stellar_dotnet_sdk.xdr.TransactionResult;

namespace Lykke.Service.Stellar.Api.Services.Horizon
{
    public class HorizonService : IHorizonService
    {
        private readonly Uri _horizonUrl;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Server _server;

        [UsedImplicitly]
        public HorizonService(AppSettings appSettings,
                              IHttpClientFactory httpClientFactory,
                              Server server)
        {
            var network = appSettings.StellarApiService.NetworkPassphrase;
            if (network != "Test SDF Network ; September 2015")
                Network.UsePublicNetwork();
            else
                Network.UseTestNetwork();

            _horizonUrl = new Uri(appSettings.StellarApiService.HorizonUrl);
            _httpClientFactory = httpClientFactory;
            _server = server;
        }

        public async Task<string> SubmitTransactionAsync(string signedTx)
        {
            // submit a tx
            var transaction = stellar_dotnet_sdk.Transaction.FromEnvelopeXdr(signedTx);
            var acc = await _server.Accounts.Account(transaction.SourceAccount.AccountId);
            var tx = await _server.SubmitTransaction(transaction);

            if (string.IsNullOrEmpty(tx?.Hash))
            {
                if (tx == null)
                {
                    throw new HorizonApiException("Submitting transaction failed. No valid transaction was returned.");
                }
                if (!tx.Result.IsSuccess)
                {
                    throw new BadRequestHorizonApiException(tx.SubmitTransactionResponseExtras.ExtrasResultCodes.TransactionResultCode, tx.SubmitTransactionResponseExtras.ExtrasResultCodes.OperationsResultCodes);
                }
            }

            return tx.Hash;
        }

        public async Task<TransactionResponse> GetTransactionDetails(string hash)
        {
            try
            {
                var builder = new TransactionsRequestBuilder(_horizonUrl, _httpClientFactory.CreateClient());
                var tx = await builder.Transaction(hash);

                return tx;
            }
            catch (NotFoundException)
            {
                // transaction not found
                return null;
            }
        }

        public async Task<List<TransactionResponse>> GetTransactions(string address,
            OrderDirection order = OrderDirection.ASC, string cursor = "", int limit = 100)
        {
            try
            {
                var builder = new TransactionsRequestBuilder(_horizonUrl, _httpClientFactory.CreateClient());
                builder.ForAccount(address);
                builder.Order(order).Cursor(cursor).Limit(limit);
                var details = await builder.Execute();
                var transactions = details?.Embedded?.Records;
                if (transactions != null)
                {
                    return transactions
                        .Where(tx => GetTransactionResult(tx) == TransactionResultCode.TransactionResultCodeEnum.txSUCCESS)
                        .ToList();
                }
            }
            catch (NotFoundException)
            {
                // address not found
            }

            return new List<TransactionResponse>();
        }

        public async Task<List<OperationResponse>> GetTransactionOperations(string hash)
        {
            var result = await new OperationsRequestBuilder(_horizonUrl, _httpClientFactory.CreateClient())
                .ForTransaction(hash)
                .Execute();

            return result?.Records;
        }

        public async Task<LedgerResponse> GetLatestLedger()
        {
            var builder = new LedgersRequestBuilder(_horizonUrl, _httpClientFactory.CreateClient());
            builder.Order(OrderDirection.DESC).Limit(1);
            var ledgers = await builder.Execute();
            if (ledgers?.Embedded?.Records == null || ledgers.Embedded?.Records.Count < 1)
            {
                throw new HorizonApiException("Latest ledger missing from query result.");
            }
            return ledgers.Embedded.Records[0];
        }

        public async Task<AccountResponse> GetAccountDetails(string address)
        {
            try
            {
                var builder = new AccountsRequestBuilder(_horizonUrl, _httpClientFactory.CreateClient());
                var accountDetails = await builder.Account(address);
                
                return accountDetails;
            }
            catch (NotFoundException)
            {
                // address not found
                return null;
            }
        }

        public async Task<bool> AccountExists(string address)
        {
            var accountDetails = await GetAccountDetails(address);
            return accountDetails != null;
        }

        public long GetAccountMergeAmount(string resultXdrBase64, int operationIndex)
        {
            var xdr = Convert.FromBase64String(resultXdrBase64);
            var txResult = TransactionResult.Decode(new XdrDataInputStream(xdr));
            var merge = txResult.Result.Results[operationIndex];
            if (merge.Tr.AccountMergeResult != null && merge.Tr.AccountMergeResult.Discriminant.InnerValue == AccountMergeResultCode.AccountMergeResultCodeEnum.ACCOUNT_MERGE_SUCCESS)
            {
                var amount = merge.Tr.AccountMergeResult.SourceAccountBalance.InnerValue;
                return amount;
            }

            return 0;
        }

        public long GetAccountMergeAmount(string metaXdrBase64, string sourceAddress)
        {
            var xdr = Convert.FromBase64String(metaXdrBase64);
            var reader = new XdrDataInputStream(xdr);
            var txMeta = TransactionMeta.Decode(reader);
            var mergeMeta = txMeta.Operations.First(op =>
            {
                return op.Changes.InnerValue.Any(c =>
                {
                    return c.Discriminant.InnerValue == LedgerEntryChangeType.LedgerEntryChangeTypeEnum.LEDGER_ENTRY_REMOVED &&
                        KeyPair.FromXdrPublicKey(c.Removed.Account.AccountID.InnerValue).Address == sourceAddress;
                });
            });
            var sourceAccountStateMeta = mergeMeta.Changes.InnerValue.First(c =>
                c.Discriminant.InnerValue == LedgerEntryChangeType.LedgerEntryChangeTypeEnum.LEDGER_ENTRY_STATE && KeyPair.FromXdrPublicKey(c.State.Data.Account.AccountID.InnerValue).Address == sourceAddress);

            return sourceAccountStateMeta.State.Data.Account.Balance.InnerValue;
        }

        public Operation.OperationBody GetFirstOperationFromTxEnvelopeXdr(string xdrBase64)
        {
            var xdr = Convert.FromBase64String(xdrBase64);
            var reader = new XdrDataInputStream(xdr);
            var txEnvelope = TransactionEnvelope.Decode(reader);
            return GetFirstOperationFromTxEnvelope(txEnvelope);
        }

        public Operation.OperationBody GetFirstOperationFromTxEnvelope(TransactionEnvelope txEnvelope)
        {
            if (txEnvelope.Discriminant.InnerValue == EnvelopeType.EnvelopeTypeEnum.ENVELOPE_TYPE_TX_V0)
            {
                if (txEnvelope?.V0?.Tx.Operations == null || txEnvelope?.V0?.Tx.Operations.Length < 1 ||
                    txEnvelope?.V0?.Tx.Operations[0].Body == null)
                {
                    throw new HorizonApiException($"Failed to extract first operation from transaction.");
                }

                var operation = txEnvelope?.V0?.Tx.Operations[0].Body;
                return operation;
            }

            if (txEnvelope.Discriminant.InnerValue == EnvelopeType.EnvelopeTypeEnum.ENVELOPE_TYPE_TX)
            {
                if (txEnvelope?.V1?.Tx.Operations == null || txEnvelope?.V1?.Tx.Operations.Length < 1 ||
                    txEnvelope?.V1?.Tx.Operations[0].Body == null)
                {
                    throw new HorizonApiException($"Failed to extract first operation from transaction.");
                }

                var operation = txEnvelope?.V1?.Tx.Operations[0].Body;
                return operation;
            }


            throw new HorizonApiException($"Failed to extract first operation from transaction.");
        }

        public string GetMemo(TransactionResponse tx)
        {
            if ((StellarSdkConstants.MemoTextTypeName.Equals(tx.MemoType, StringComparison.OrdinalIgnoreCase) ||
                StellarSdkConstants.MemoIdTypeName.Equals(tx.MemoType, StringComparison.OrdinalIgnoreCase)) &&
                !string.IsNullOrEmpty(tx.Memo.ToXdr().Text))
            {
                return tx.Memo.ToXdr().Text;
            }

            return null;
        }
        public TransactionResultCode.TransactionResultCodeEnum GetTransactionResult(TransactionResponse tx)
        {
            var xdr = Convert.FromBase64String(tx.ResultXdr);
            var reader = new XdrDataInputStream(xdr);
            var txResult = TransactionResult.Decode(reader);
            
            return txResult.Result.Discriminant.InnerValue;
        }
    }
}
