using System;
using System.Linq;
using System.Threading.Tasks;
using StellarGenerated = Stellar.Generated;
using StellarSdk;
using StellarSdk.Model;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Service.Stellar.Api.Services.Horizon
{
    public class HorizonService: IHorizonService
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

        public async Task<TransactionDetails> GetTransactionDetails(string transactionHash)
        {
            var builder = new TransactionCallBuilder(_horizonUrl);
            builder.transaction(transactionHash);
            var tx = await builder.Call();
            return tx;
        }

        public async Task<Payments> GetPayments(string address, string order = "asc", string cursor = "")
        {
            var builder = new PaymentCallBuilder(_horizonUrl);
            builder.accountId(address);
            builder.order(order).cursor(cursor);
            var payments = await builder.Call();
            return payments;
        }

        public async Task<LedgerDetails> GetLatestLedger()
        {
            var builder = new LedgerCallBuilder(_horizonUrl);
            builder.order("desc").limit(1);
            var ledgers = await builder.Call();
            if (ledgers?.Embedded?.Records == null || ledgers?.Embedded?.Records.Length < 1)
            {
                throw new HorizonApiException($"Latest ledger missing from query result.");
            }
            return ledgers.Embedded.Records[0];
        }

        public async Task<AccountDetails> GetAccountDetails(string address)
        {
            var builder = new AccountCallBuilder(_horizonUrl);
            builder.accountId(address);
            var accountDetails = await builder.Call();
            return accountDetails;
        }

        public async Task<long> GetLedgerNoOfLastPayment(string address)
        {
            var builder = new PaymentCallBuilder(_horizonUrl);
            builder.accountId(address);
            builder.order("desc").limit(1);
            var payments = await builder.Call();
            if (payments?.Embedded?.Records == null || payments?.Embedded?.Records.Length < 1)
            {
                throw new HorizonApiException($"Latest ledger missing from query result.");
            }
            var tx = await GetTransactionDetails(payments.Embedded.Records[0].TransactionHash);
            return tx.Ledger;
        }

        public long GetAccountMergeAmount(string resultXdrBase64, int accountMergeInTx)
        {
            var xdr = Convert.FromBase64String(resultXdrBase64);
            var reader = new StellarGenerated.ByteReader(xdr);
            var txResult = StellarGenerated.TransactionResult.Decode(reader);

            var merges = txResult.Result.Results.Where(x => x.Tr.AccountMergeResult != null).ToList();
            if (merges.Count > accountMergeInTx)
            {
                var merge = merges[accountMergeInTx];
                var result = merge?.Tr?.AccountMergeResult;
                var resultCode = result?.Discriminant?.InnerValue;
                if (resultCode != null && resultCode == StellarGenerated.AccountMergeResultCode.AccountMergeResultCodeEnum.ACCOUNT_MERGE_SUCCESS)
                {
                    long amount = result.SourceAccountBalance.InnerValue;
                    return amount;
                }
            }
            else
            {
                throw new HorizonApiException($"Account merge result missing from result XDR (account merge no: {accountMergeInTx}.");
            }

            return 0;
        }
    }
}
