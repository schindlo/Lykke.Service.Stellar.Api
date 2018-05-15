using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StellarBase;
using StellarBase.Generated;
using StellarSdk;
using StellarSdk.Model;
using StellarSdk.Exceptions;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core;
using Chaos.NaCl;
using JetBrains.Annotations;

namespace Lykke.Service.Stellar.Api.Services.Horizon
{
    public class HorizonService : IHorizonService
    {
        private readonly string _horizonUrl;

        [UsedImplicitly]
        public HorizonService(string network,
                              string horizonUrl)
        {
            Network.CurrentNetwork = network;
            _horizonUrl = horizonUrl;
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
                    return new List<TransactionDetails>(transactions);
                }
            }
            catch (ResourceNotFoundException)
            {
                // address not found
            }

            return new List<TransactionDetails>();
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
            if (resultCode != null && resultCode == AccountMergeResultCode.AccountMergeResultCodeEnum.ACCOUNT_MERGE_SUCCESS)
            {
                long amount = result.SourceAccountBalance.InnerValue;
                return amount;
            }

            return 0;
        }

        public PaymentOp GetFirstPaymentFromTransaction(TransactionDetails tx)
        {
            var xdr = Convert.FromBase64String(tx.EnvelopeXdr);
            var reader = new ByteReader(xdr);
            var txEnvelope = TransactionEnvelope.Decode(reader);
            if (txEnvelope?.Tx?.Operations == null || txEnvelope.Tx.Operations.Length < 1 ||
                txEnvelope.Tx.Operations[0].Body?.PaymentOp == null)
            {
                throw new HorizonApiException($"Failed to extract first payment operation from transaction. hash={tx.Hash}");
            }

            var paymentOp = txEnvelope.Tx.Operations[0].Body.PaymentOp;
            return paymentOp;
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
    }
}
