using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StellarBase;
using StellarBase.Generated;
using StellarSdk.Model;
using Common.Log;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core;

namespace Lykke.Service.Stellar.Api.Services.Transaction
{
    public class TransactionHistoryService : ITransactionHistoryService
    {
        private int _batchSize;

        private string _lastJobError;

        private readonly IHorizonService _horizonService;
        private readonly IObservationRepository<TransactionHistoryObservation> _observationRepository;
        private readonly ITxHistoryRepository _txHistoryRepository;
        private readonly ITxBroadcastRepository _txBroadcastRepository;
        private readonly ILog _log;

        public TransactionHistoryService(IHorizonService horizonService, IObservationRepository<TransactionHistoryObservation> observationRepository,
                                         ITxHistoryRepository txHistoryRepository, ITxBroadcastRepository txBroadcastRepository, ILog log, int batchSize)
        {
            _horizonService = horizonService;
            _observationRepository = observationRepository;
            _txHistoryRepository = txHistoryRepository;
            _txBroadcastRepository = txBroadcastRepository;
            _log = log;
            _batchSize = batchSize;
        }

        public async Task<bool> IsIncomingTransactionObservedAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            return observation != null && observation.IsIncomingObserved;
        }

        public async Task<bool> IsOutgoingTransactionObservedAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            return observation != null && observation.IsOutgoingObserved;
        }

        private TransactionHistoryObservation CreateTransactionObservation(string address)
        {
            var observation = new TransactionHistoryObservation
            {
                Address = address,
                TableId = Guid.NewGuid().ToString("N").ToUpper()
            };
            return observation;
        }

        public async Task AddIncomingTransactionObservationAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            if (observation == null)
            {
                observation = CreateTransactionObservation(address);
            }
            observation.IsIncomingObserved = true;

            await _observationRepository.InsertOrReplaceAsync(observation);
        }

        public async Task AddOutgoingTransactionObservationAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            if (observation == null)
            {
                observation = CreateTransactionObservation(address);
            }
            observation.IsOutgoingObserved = true;

            await _observationRepository.InsertOrReplaceAsync(observation);
        }

        public async Task DeleteIncomingTransactionObservationAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            if (observation == null)
            {
                // nothing to do
                return;
            }
            observation.IsIncomingObserved = false;
            if (observation.IsIncomingObserved == false && observation.IsOutgoingObserved == false)
            {
                await _txHistoryRepository.DeleteAsync(observation.TableId);
                await _observationRepository.DeleteIfExistAsync(address);
            }
            else
            {
                await _observationRepository.InsertOrReplaceAsync(observation);
            }
        }

        public async Task DeleteOutgoingTransactionObservationAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            if (observation == null)
            {
                // nothing to do
                return;
            }
            observation.IsOutgoingObserved = false;
            if (observation.IsIncomingObserved == false && observation.IsOutgoingObserved == false)
            {
                await _txHistoryRepository.DeleteAsync(observation.TableId);
                await _observationRepository.DeleteIfExistAsync(address);
            }
            else
            {
                await _observationRepository.InsertOrReplaceAsync(observation);
            }
        }

        public async Task<List<TxHistory>> GetHistory(TxDirectionType direction, string address, int take, string afterHash)
        {
            var observation = await _observationRepository.GetAsync(address);
            if (observation != null)
            {
                var result = await _txHistoryRepository.GetAllAfterHashAsync(observation.TableId, direction, take, afterHash);
                return result;
            }

            return new List<TxHistory>();
        }

        public async Task<int> UpdateTransactionHistory()
        {
            int count = 0;
            try
            {
                string continuationToken = null;
                do
                {
                    var observations = await _observationRepository.GetAllAsync(_batchSize, continuationToken);
                    foreach (var item in observations.Items)
                    {
                        count += await ProcessTransactionObservation(item);
                    }
                    continuationToken = observations.ContinuationToken;
                } while (continuationToken != null);

                _lastJobError = null;
            }
            catch (Exception ex)
            {
                _lastJobError = _lastJobError = $"Error in job {nameof(TransactionHistoryService)}.{nameof(UpdateTransactionHistory)}: {ex.Message}";
                await _log.WriteErrorAsync(nameof(TransactionHistoryService), nameof(UpdateTransactionHistory),
                    "Failed to execute transaction history update", ex);
            }
            return count;
        }

        public string GetLastJobError()
        {
            return _lastJobError;
        }

        private async Task<int> QueryAndProcessTransactions(string address, TransactionContext context)
        {
            var transactions = await _horizonService.GetTransactions(address, StellarSdkConstants.OrderAsc, context.Cursor);

            int count = 0;
            context.Cursor = null;
            foreach (var transaction in transactions)
            {
                try
                {
                    context.Cursor = transaction.PagingToken;
                    count++;

                    var xdr = Convert.FromBase64String(transaction.EnvelopeXdr);
                    var reader = new ByteReader(xdr);
                    var txEnvelope = TransactionEnvelope.Decode(reader);
                    var tx = txEnvelope.Tx;

                    var memo = _horizonService.GetMemo(transaction);
                    for (short i = 0; i < tx.Operations.Length; i++)
                    {
                        var operation = tx.Operations[i];
                        var operationType = operation.Body.Discriminant.InnerValue;

                        var history = new TxHistory
                        {
                            FromAddress = transaction.SourceAccount,
                            AssetId = Core.Domain.Asset.Stellar.Id,
                            Hash = transaction.Hash,
                            OperationIndex = i,
                            PagingToken = transaction.PagingToken,
                            CreatedAt = transaction.CreatedAt,
                            Memo = memo
                        };

                        if (operationType == OperationType.OperationTypeEnum.CREATE_ACCOUNT)
                        {
                            var op = operation.Body.CreateAccountOp;
                            var keyPair = KeyPair.FromXdrPublicKey(op.Destination.InnerValue);
                            history.ToAddress = keyPair.Address;
                            history.Amount = op.StartingBalance.InnerValue;
                            history.PaymentType = PaymentType.CreateAccount;
                        }
                        else if (operationType == OperationType.OperationTypeEnum.PAYMENT)
                        {
                            var op = operation.Body.PaymentOp;
                            if (op.Asset.Discriminant.InnerValue == AssetType.AssetTypeEnum.ASSET_TYPE_NATIVE)
                            {
                                var keyPair = KeyPair.FromXdrPublicKey(op.Destination.InnerValue);
                                history.ToAddress = keyPair.Address;
                                history.Amount = op.Amount.InnerValue;
                                history.PaymentType = PaymentType.Payment;
                            }
                        }
                        else if (operationType == OperationType.OperationTypeEnum.ACCOUNT_MERGE)
                        {
                            var op = operation.Body;
                            var keyPair = KeyPair.FromXdrPublicKey(op.Destination.InnerValue);
                            history.ToAddress = keyPair.Address;
                            history.Amount = _horizonService.GetAccountMergeAmount(transaction.ResultXdr, i);
                            history.PaymentType = PaymentType.AccountMerge;
                        }

                        if (address.Equals(history.ToAddress, StringComparison.OrdinalIgnoreCase))
                        {
                            await _txHistoryRepository.InsertOrReplaceAsync(context.TableId, TxDirectionType.Incoming, history);

                        }
                        if (address.Equals(history.FromAddress, StringComparison.OrdinalIgnoreCase))
                        {
                            await _txHistoryRepository.InsertOrReplaceAsync(context.TableId, TxDirectionType.Outgoing, history);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new BusinessException($"Failed to process transaction. hash={transaction?.Hash}", ex);
                }
            }

            if (!string.IsNullOrEmpty(context.Cursor))
            {
                await _txHistoryRepository.SetCurrentPagingToken(context.TableId, context.Cursor);
            }

            return count;
        }

        private async Task<int> ProcessTransactionObservation(TransactionHistoryObservation observation)
        {
            int count = 0;
            try
            {
                var context = new TransactionContext(observation.TableId);
                context.Cursor = await _txHistoryRepository.GetCurrentPagingToken(context.TableId);

                do
                {
                    count += await QueryAndProcessTransactions(observation.Address, context);
                }
                while (!string.IsNullOrEmpty(context.Cursor));
            }
            catch (Exception ex)
            {
                await _log.WriteErrorAsync(nameof(TransactionHistoryService), nameof(ProcessTransactionObservation),
                    $"Failed to process transaction observation for address. address={observation.Address}", ex);
            }
            return count;
        }
    }
}
