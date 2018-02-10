using System;
using System.Linq;
using System.Threading.Tasks;
using StellarBase.Generated;
using StellarSdk;
using StellarSdk.Model;
using StellarSdk.Exceptions;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core;

namespace Lykke.Service.Stellar.Api.Services.Horizon
{
    public class HorizonService : IHorizonService
    {
        private readonly string _horizonUrl;

        public HorizonService(string horizonUrl)
        {
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
                throw new HorizonApiException($"Submitting transaction failed. No valid transaction was returned.");
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

        public async Task<Payments> GetPayments(string address, string order = StellarSdkConstants.OrderAsc, string cursor = "")
        {
            try
            {
                var builder = new PaymentCallBuilder(_horizonUrl);
                builder.accountId(address);
                builder.order(order).cursor(cursor);
                var payments = await builder.Call();
                return payments;
            }
            catch (ResourceNotFoundException)
            {
                // address not found
                return null;
            }
        }

        public async Task<LedgerDetails> GetLatestLedger()
        {
            var builder = new LedgerCallBuilder(_horizonUrl);
            builder.order(StellarSdkConstants.OrderDesc).limit(1);
            var ledgers = await builder.Call();
            if (ledgers?.Embedded?.Records == null || ledgers?.Embedded?.Records.Length < 1)
            {
                throw new HorizonApiException($"Latest ledger missing from query result.");
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

        public async Task<long> GetLedgerNoOfLastPayment(string address)
        {
            var builder = new PaymentCallBuilder(_horizonUrl);
            builder.accountId(address);
            builder.order(StellarSdkConstants.OrderDesc).limit(1);
            var payments = await builder.Call();
            if (payments?.Embedded?.Records == null || payments?.Embedded?.Records.Length < 1)
            {
                throw new HorizonApiException($"Latest ledger missing from query result.");
            }
            var hash = payments.Embedded.Records[0].TransactionHash;
            var tx = await GetTransactionDetails(hash);
            if (tx == null)
            {
                throw new HorizonApiException($"Transaction not found. hash={hash}");
            }
            return tx.Ledger;
        }

        public long GetAccountMergeAmount(string resultXdrBase64, int accountMergeInTx)
        {
            var xdr = Convert.FromBase64String(resultXdrBase64);
            var reader = new ByteReader(xdr);
            var txResult = StellarBase.Generated.TransactionResult.Decode(reader);

            var merges = txResult.Result.Results.Where(x => x.Tr.AccountMergeResult != null).ToList();
            if (merges.Count > accountMergeInTx)
            {
                var merge = merges[accountMergeInTx];
                var result = merge?.Tr?.AccountMergeResult;
                var resultCode = result?.Discriminant?.InnerValue;
                if (resultCode != null && resultCode == AccountMergeResultCode.AccountMergeResultCodeEnum.ACCOUNT_MERGE_SUCCESS)
                {
                    long amount = result.SourceAccountBalance.InnerValue;
                    return amount;
                }
            }
            else
            {
                throw new HorizonApiException($"Account merge result missing from result XDR. accountMergeInTx={accountMergeInTx}");
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
    }
}
