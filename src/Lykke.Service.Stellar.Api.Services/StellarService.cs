using System;
using System.Threading.Tasks;
using StellarBase = Stellar;
using StellarGenerated = Stellar.Generated;
using StellarSdk;
using StellarSdk.Model;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Service.Stellar.Api.Services
{
    public class StellarService: IStellarService
    {
        private string _horizonUrl = "https://horizon-testnet.stellar.org/";

        private readonly ITxBroadcastRepository _broadcastRepository;

        public StellarService(ITxBroadcastRepository broadcastRepository)
        {
            _broadcastRepository = broadcastRepository;
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
                    Timestamp = tx.CreatedAt,
                    Amount = paymentOp.Amount.InnerValue,
                    Fee = tx.FeePaid,
                    Hash = tx.Hash,
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
                    Timestamp = DateTime.UtcNow,
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

        public async Task<long> GetAddressBalanceAsync(string address, bool excludeMinBalance)
        {
            // TODO
            return 0;
        }

        public async Task<long> GetFeeAsync()
        {
            // TODO: use dynamic fee
            return 100;
        }

        public async Task<string> BuildTransactionAsync(Guid operationId, string fromAddress, string toAddress, long amount)
        {
            // TODO
            return string.Empty;
        }
    }
}
