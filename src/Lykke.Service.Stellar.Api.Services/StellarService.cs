using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using StellarBase = Stellar;
using StellarGenerated = Stellar.Generated;
using StellarSdk;
using StellarSdk.Model;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Domain;

namespace Lykke.Service.Stellar.Api.Services
{
    public class StellarService: IStellarService
    {
        private string _horizonUrl = null;

        private readonly ITxBroadcastRepository _broadcastRepository;
        private readonly ITxBuildRepository _buildRepository;

        public StellarService(ITxBroadcastRepository broadcastRepository, ITxBuildRepository buildRepository, string horizonUrl)
        {
            _broadcastRepository = broadcastRepository;
            _buildRepository = buildRepository;
            _horizonUrl = horizonUrl;
        }

        public Boolean IsAddressValid(string address)
        {
            try
            {
                StellarBase.StrKey.DecodeCheck(StellarBase.VersionByte.ed25519Publickey, address);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<TxBroadcast> GetTxBroadcastAsync(Guid operationId)
        {
            return await _broadcastRepository.GetAsync(operationId);
        }

        public async Task BroadcastTxAsync(Guid operationId, string xdrBase64)
        {
            try
            {
                var tx = await SubmitTransactionAsync(xdrBase64);

                // TODO: Move to stellar-base
                var xdr = Convert.FromBase64String(tx.EnvelopeXdr);
                var reader = new StellarGenerated.ByteReader(xdr);
                var txEnvelope = StellarGenerated.TransactionEnvelope.Decode(reader);
                var paymentOp = txEnvelope.Tx.Operations[0].Body.PaymentOp;

                var broadcast = new TxBroadcast
                {
                    OperationId = operationId,
                    State = TxBroadcastState.Completed,
                    Amount = paymentOp.Amount.InnerValue,
                    Fee = tx.FeePaid,
                    Hash = tx.Hash,
                    CreatedAt = tx.CreatedAt,
                    Ledger = tx.Ledger
                };
                await _broadcastRepository.AddAsync(broadcast);
            }
            catch (Exception ex)
            {
                var broadcast = new TxBroadcast
                {
                    OperationId = operationId,
                    State = TxBroadcastState.Failed,
                    Error = ex.Message,
                    // TODO: set correct error
                    ErrorCode = TxExecutionError.Unknown
                };
                await _broadcastRepository.AddAsync(broadcast);

                throw new ServiceException($"Broadcasting transaction failed (operationId: {operationId}).", ex);
            }
        }

        public async Task DeleteTxBroadcastAsync(Guid operationId)
        {
            await _broadcastRepository.DeleteAsync(operationId);
        }

        private async Task<TransactionDetails> SubmitTransactionAsync(string signedTx)
        {
            // submit a tx
            var builder = new TransactionCallBuilder(_horizonUrl);
            builder.submitTransaction(signedTx);
            var tx = await builder.Call();
            if (tx == null || string.IsNullOrEmpty(tx.Hash))
            {
                throw new HorizonApiException($"Submitting transaction failed. No valid transaction was returned.");
            }

            // read details of this tx
            builder = new TransactionCallBuilder(_horizonUrl);
            builder.transaction(tx.Hash);
            var txDetails = await builder.Call();
            return txDetails;
        }

        public async Task<Fees> GetFeesAsync()
        {
            LedgerCallBuilder builder = new LedgerCallBuilder(_horizonUrl);
            builder.order("desc").limit(1);
            var ledgers = await builder.Call();

            var latest = ledgers.Embedded.Records[0];
            var fees = new Fees
            {
                BaseFee = latest.BaseFee,
                BaseReserve = Convert.ToDecimal(latest.BaseReserve)
            };
            return fees;
        }

        public async Task<TxBuild> GetTxBuildAsync(Guid operationId)
        {
            return await _buildRepository.GetAsync(operationId);
        }

        public async Task<string> BuildTransactionAsync(Guid operationId, AddressBalance from, string toAddress, long amount)
        {
            var fromKeyPair = StellarBase.KeyPair.FromAddress(from.Address);
            var fromAccount = new StellarBase.Account(fromKeyPair, from.Sequence);

            var toKeyPair = StellarBase.KeyPair.FromAddress(toAddress);

            var asset = new StellarBase.Asset();
            var operation = new StellarBase.PaymentOperation.Builder(toKeyPair, asset, amount)
                                           .SetSourceAccount(fromKeyPair)
                                           .Build();

            fromAccount.IncrementSequenceNumber();

            var tx = new StellarBase.Transaction.Builder(fromAccount)
                                    .AddOperation(operation)
                                    .Build();

            var xdr = tx.ToXDR();
            var writer = new StellarGenerated.ByteWriter();
            StellarGenerated.Transaction.Encode(writer, xdr);
            var xdrBase64 = Convert.ToBase64String(writer.ToArray());

            var build = new TxBuild
            {
                OperationId = operationId,
                Timestamp = DateTimeOffset.UtcNow,
                XdrBase64 = xdrBase64
            };
            await _buildRepository.AddAsync(build);

            return xdrBase64;
        }
    }
}
