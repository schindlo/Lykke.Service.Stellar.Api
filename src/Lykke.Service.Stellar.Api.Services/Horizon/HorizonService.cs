extern alias sdk2;
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
using sdk2::stellar_dotnet_sdk.requests;
using sdk2::stellar_dotnet_sdk.responses.operations;
using StellarBase;
using StellarBase.Generated;
using StellarSdk;
using StellarSdk.Exceptions;
using StellarSdk.Model;
using static StellarBase.Generated.LedgerEntryChangeType;
using static StellarBase.Generated.Operation;

namespace Lykke.Service.Stellar.Api.Services.Horizon
{
    public class HorizonService : IHorizonService
    {
        private readonly string _horizonUrl;
        private readonly IHttpClientFactory _httpClientFactory;

        [UsedImplicitly]
        public HorizonService(string network,
                              string horizonUrl,
                              IHttpClientFactory httpClientFactory)
        {
            Network.CurrentNetwork = network;
            _horizonUrl = horizonUrl;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> SubmitTransactionAsync(string signedTx)
        {
            // submit a tx
            var builder = new TransactionCallBuilder(_horizonUrl);
            builder.submitTransaction(signedTx);
            var tx = await builder.Call();
            if (tx == null || string.IsNullOrEmpty(tx.Hash))
            {
                throw new HorizonApiException("Submitting transaction failed. No valid transaction was returned.");
            }
            return tx.Hash;
        }

        public async Task<TransactionDetails> GetTransactionDetails(string hash)
        {
            try
            {
                var builder = new TransactionCallBuilder(_horizonUrl);
                builder.transaction(hash);
                var tx = await builder.Call();
                return tx;
            }
            catch (ResourceNotFoundException)
            {
                // transaction not found
                return null;
            }
        }

        public async Task<List<TransactionDetails>> GetTransactions(string address, string order = StellarSdkConstants.OrderAsc, string cursor = "", int limit = 100)
        {
            try
            {
                var builder = new AccountTransactionCallBuilder(_horizonUrl);
                builder.accountId(address);
                builder.order(order).cursor(cursor).limit(limit);
                var details = await builder.Call();
                var transactions = details?.Embedded?.Records;
                if (transactions != null)
                {
                    return transactions
                        .Where(tx => GetTransactionResult(tx) == TransactionResultCode.TransactionResultCodeEnum.txSUCCESS)
                        .ToList();
                }
            }
            catch (ResourceNotFoundException)
            {
                // address not found
            }

            return new List<TransactionDetails>();
        }

        public async Task<List<OperationResponse>> GetTransactionOperations(string hash)
        {
            var result = await new OperationsRequestBuilder(new Uri(_horizonUrl), _httpClientFactory.CreateClient())
                .ForTransaction(hash)
                .Execute();

            return result?.Records;
        }

        public async Task<LedgerDetails> GetLatestLedger()
        {
            var builder = new LedgerCallBuilder(_horizonUrl);
            builder.order(StellarSdkConstants.OrderDesc).limit(1);
            var ledgers = await builder.Call();
            if (ledgers?.Embedded?.Records == null || ledgers.Embedded?.Records.Length < 1)
            {
                throw new HorizonApiException("Latest ledger missing from query result.");
            }
            return ledgers.Embedded.Records[0];
        }

        public async Task<AccountDetails> GetAccountDetails(string address)
        {
            try
            {
                var builder = new AccountCallBuilder(_horizonUrl);
                builder.accountId(address);
                var accountDetails = await builder.Call();
                return accountDetails;
            }
            catch (ResourceNotFoundException)
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
            var reader = new ByteReader(xdr);
            var txResult = StellarBase.Generated.TransactionResult.Decode(reader);
            var merge = txResult.Result.Results[operationIndex];
            var result = merge?.Tr?.AccountMergeResult;
            var resultCode = result?.Discriminant?.InnerValue;
            if (resultCode == null ||
                resultCode != AccountMergeResultCode.AccountMergeResultCodeEnum.ACCOUNT_MERGE_SUCCESS) return 0;

            var amount = result.SourceAccountBalance.InnerValue;
            return amount;
        }

        public long GetAccountMergeAmount(string metaXdrBase64, string sourceAddress)
        {
            var xdr = Convert.FromBase64String(metaXdrBase64);
            var reader = new ByteReader(xdr);
            var txMeta = StellarBase.Generated.TransactionMeta.Decode(reader);
            var mergeMeta = txMeta.Operations.First(op =>
            {
                return op.Changes.InnerValue.Any(c =>
                {
                    return c.Discriminant.InnerValue == LedgerEntryChangeTypeEnum.LEDGER_ENTRY_REMOVED &&
                        KeyPair.FromXdrPublicKey(c.Removed.Account.AccountID.InnerValue).Address == sourceAddress;
                });
            });
            var sourceAccountStateMeta = mergeMeta.Changes.InnerValue.First(c => 
                c.Discriminant.InnerValue == LedgerEntryChangeTypeEnum.LEDGER_ENTRY_STATE && KeyPair.FromXdrPublicKey(c.State.Data.Account.AccountID.InnerValue).Address == sourceAddress);

            return sourceAccountStateMeta.State.Data.Account.Balance.InnerValue;
        }

        public OperationBody GetFirstOperationFromTxEnvelopeXdr(string xdrBase64)
        {
            var xdr = Convert.FromBase64String(xdrBase64);
            var reader = new ByteReader(xdr);
            var txEnvelope = TransactionEnvelope.Decode(reader);
            return GetFirstOperationFromTxEnvelope(txEnvelope);
        }

        public OperationBody GetFirstOperationFromTxEnvelope(TransactionEnvelope txEnvelope)
        {
            if (txEnvelope?.Tx?.Operations == null || txEnvelope.Tx.Operations.Length < 1 ||
                txEnvelope.Tx.Operations[0].Body == null)
            {
                throw new HorizonApiException($"Failed to extract first operation from transaction.");
            }

            var operation = txEnvelope.Tx.Operations[0].Body;
            return operation;
        }

        public string GetMemo(TransactionDetails tx)
        {
            if ((StellarSdkConstants.MemoTextTypeName.Equals(tx.MemoType, StringComparison.OrdinalIgnoreCase) ||
                StellarSdkConstants.MemoIdTypeName.Equals(tx.MemoType, StringComparison.OrdinalIgnoreCase)) &&
                !string.IsNullOrEmpty(tx.Memo))
            {
                return tx.Memo;
            }

            return null;
        }

        public string GetTransactionHash(StellarBase.Generated.Transaction tx)
        {
            var writer = new ByteWriter();

            // Hashed NetworkID
            writer.Write(Network.CurrentNetworkId);

            // Envelope Type - 4 bytes
            EnvelopeType.Encode(writer, EnvelopeType.Create(EnvelopeType.EnvelopeTypeEnum.ENVELOPE_TYPE_TX));

            // Transaction XDR bytes
            var txWriter = new ByteWriter();
            StellarBase.Generated.Transaction.Encode(txWriter, tx);
            writer.Write(txWriter.ToArray());

            var data = writer.ToArray();
            var hash = Utilities.Hash(data);
            return CryptoBytes.ToHexStringLower(hash);
        }

        public TransactionResultCode.TransactionResultCodeEnum GetTransactionResult(TransactionDetails tx)
        {
            var xdr = Convert.FromBase64String(tx.ResultXdr);
            var reader = new ByteReader(xdr);
            var txResult = StellarBase.Generated.TransactionResult.Decode(reader);
            return txResult.Result.Discriminant.InnerValue;
        }
    }
}
