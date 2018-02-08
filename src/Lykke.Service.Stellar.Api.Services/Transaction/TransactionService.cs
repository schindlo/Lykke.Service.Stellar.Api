using System;
using System.Threading.Tasks;
using StellarBase = Stellar;
using StellarGenerated = Stellar.Generated;
using StellarSdk.Exceptions;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;

namespace Lykke.Service.Stellar.Api.Services.Transaction
{
    public class TransactionService : ITransactionService
    {
        private const int BatchSize = 100;

        private string _lastJobError;

        private readonly IHorizonService _horizonService;
        private readonly IObservationRepository<BroadcastObservation> _observationRepository;
        private readonly ITxBroadcastRepository _broadcastRepository;
        private readonly ITxBuildRepository _buildRepository;
        private readonly ILog _log;

        public TransactionService(IHorizonService horizonService, IObservationRepository<BroadcastObservation> observationRepository,
                                  ITxBroadcastRepository broadcastRepository, ITxBuildRepository buildRepository, ILog log)
        {
            _horizonService = horizonService;
            _observationRepository = observationRepository;
            _broadcastRepository = broadcastRepository;
            _buildRepository = buildRepository;
            _log = log;
        }

        public async Task<TxBroadcast> GetTxBroadcastAsync(Guid operationId)
        {
            return await _broadcastRepository.GetAsync(operationId);
        }

        public async Task BroadcastTxAsync(Guid operationId, string xdrBase64)
        {
            try
            {
                var hash = await _horizonService.SubmitTransactionAsync(xdrBase64);
                var broadcast = new TxBroadcast
                {
                    OperationId = operationId,
                    State = TxBroadcastState.InProgress,
                    Hash = hash
                };
                await _broadcastRepository.InsertOrReplaceAsync(broadcast);
                var observation = new BroadcastObservation
                {
                    OperationId = operationId
                };
                await _observationRepository.InsertOrReplaceAsync(observation);
            }
            catch (Exception ex)
            {
                var broadcast = new TxBroadcast
                {
                    OperationId = operationId,
                    State = TxBroadcastState.Failed,
                    Error = ex.Message,
                    ErrorCode = GetErrorCode(ex)
                };
                await _broadcastRepository.InsertOrReplaceAsync(broadcast);

                var be = new BusinessException($"Broadcasting transaction failed (operationId: {operationId}).", ex);
                be.Data.Add("ErrorCode", broadcast.ErrorCode);
                throw be;
            }
        }

        private TxExecutionError GetErrorCode(Exception ex)
        {
            if (ex.GetType() == typeof(BadRequestException))
            {
                var bre = (BadRequestException)ex;
                var ops = bre.ErrorDetails?.Extras?.ResultCodes?.Operations;
                if (bre.ErrorDetails.Status == 400 && ops != null && ops.Length > 0 && ops[0].Equals("op_underfunded"))
                {
                    return TxExecutionError.NotEnoughtBalance;
                }
            }
            return TxExecutionError.Unknown;
        }

        public async Task DeleteTxBroadcastAsync(Guid operationId)
        {
            await _broadcastRepository.DeleteAsync(operationId);
        }

        public async Task<Fees> GetFeesAsync()
        {
            var latest = await _horizonService.GetLatestLedger();
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
                XdrBase64 = xdrBase64
            };
            await _buildRepository.AddAsync(build);

            return xdrBase64;
        }

        public string GetLastJobError()
        {
            return _lastJobError;
        }

        public async Task UpdateBroadcastsInProgress()
        {
            try
            {
                string continuationToken = null;
                do
                {
                    var observations = await _observationRepository.GetAllAsync(BatchSize, continuationToken);
                    foreach (var item in observations.Items)
                    {
                        await ProcessBroadcastInProgress(item.OperationId);
                    }
                    continuationToken = observations.ContinuationToken;
                } while (continuationToken != null);

                _lastJobError = null;
            }
            catch (Exception ex)
            {
                _lastJobError = $"Error in job {nameof(TransactionService)}.{nameof(UpdateBroadcastsInProgress)}: {ex.Message}";
                await _log.WriteErrorAsync(nameof(TransactionService), nameof(UpdateBroadcastsInProgress),
                    "Failed to execute broadcast in progress update", ex);
            }
        }

        private async Task ProcessBroadcastInProgress(Guid operationId)
        {
            try
            {
                var broadcast = await _broadcastRepository.GetAsync(operationId);
                if (broadcast == null)
                {
                    throw new BusinessException($"Broadcast for observed operation not found (operation id: {operationId}).");
                }

                var tx = await _horizonService.GetTransactionDetails(broadcast.Hash);
                if (tx == null)
                {
                    // transaction still in progress
                    return;
                }

                var paymentOp = _horizonService.GetFirstPaymentFromTransaction(tx);
                broadcast.State = TxBroadcastState.Completed;
                broadcast.Amount = paymentOp.Amount.InnerValue;
                broadcast.Fee = tx.FeePaid;
                broadcast.CreatedAt = tx.CreatedAt;
                broadcast.Ledger = tx.Ledger;
                await _broadcastRepository.InsertOrReplaceAsync(broadcast);
                await _observationRepository.DeleteIfExistAsync(operationId.ToString());
            }
            catch (Exception ex)
            {
                var broadcast = new TxBroadcast
                {
                    OperationId = operationId,
                    State = TxBroadcastState.Failed,
                    Error = ex.Message,
                    ErrorCode = TxExecutionError.Unknown
                };
                await _broadcastRepository.InsertOrReplaceAsync(broadcast);
                await _observationRepository.DeleteIfExistAsync(operationId.ToString());

                await _log.WriteErrorAsync(nameof(TransactionService), nameof(ProcessBroadcastInProgress),
                                           $"Failed to process in progress broadcast (operation id: {operationId}).", ex);
            }
       }
    }
}
