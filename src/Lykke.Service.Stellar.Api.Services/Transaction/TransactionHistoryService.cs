using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Common.Log;
using StellarBase;
using StellarBase.Generated;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Utils;
using Lykke.Service.Stellar.Api.Services.Assets;

namespace Lykke.Service.Stellar.Api.Services.Transaction
{
    public class TransactionHistoryService : ITransactionHistoryService
    {
        private string _lastJobError;

        private readonly IBalanceService _balanceService;
        private readonly IHorizonService _horizonService;
        private readonly IKeyValueStoreRepository _keyValueStoreRepository;
        private readonly IObservationRepository<TransactionHistoryObservation> _observationRepository;
        private readonly ITxHistoryRepository _txHistoryRepository;
        private readonly ILog _log;
        private readonly IBlockchainAssetsService _blockchainAssetsService;

        [UsedImplicitly]
        public TransactionHistoryService(IBalanceService balanceService,
                                         IHorizonService horizonService,
                                         IKeyValueStoreRepository keyValueStoreRepository,
                                         IObservationRepository<TransactionHistoryObservation> observationRepository,
                                         ITxHistoryRepository txHistoryRepository,
                                         ILogFactory log,
                                         IBlockchainAssetsService blockchainAssetsService)
        {
            _balanceService = balanceService;
            _horizonService = horizonService;
            _keyValueStoreRepository = keyValueStoreRepository;
            _observationRepository = observationRepository;
            _txHistoryRepository = txHistoryRepository;
            _log = log.CreateLog(this);
            _blockchainAssetsService = blockchainAssetsService;
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

        public async Task AddIncomingTransactionObservationAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            // ReSharper disable once ConvertIfStatementToNullCoalescingExpression
            if (observation == null)
            {
                observation = new TransactionHistoryObservation
                {
                    Address = address
                };
            }
            observation.IsIncomingObserved = true;

            await _observationRepository.InsertOrReplaceAsync(observation);
        }

        public async Task AddOutgoingTransactionObservationAsync(string address)
        {
            var observation = await _observationRepository.GetAsync(address);
            // ReSharper disable once ConvertIfStatementToNullCoalescingExpression
            if (observation == null)
            {
                observation = new TransactionHistoryObservation
                {
                    Address = address
                };
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
            if (observation == null)
            {
                return new List<TxHistory>();
            }

            List<TxHistory> result;
            string afterPagingToken = null;
            if (!string.IsNullOrEmpty(afterHash))
            {
                var tx = await _horizonService.GetTransactionDetails(afterHash);
                if (tx == null || !afterHash.Equals(tx.Hash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessException($"No transaction found. hash={afterHash}");
                }
                afterPagingToken = tx.PagingToken;
            }

            if (_balanceService.IsDepositBaseAddress(address))
            {
                result = await GetDepositBaseHistory(direction, address, take, afterPagingToken);
            }
            else
            {
                result = await GetBaseAddressHistory(direction, address, take, afterPagingToken);
            }

            return result;
        }

        private async Task<List<TxHistory>> GetDepositBaseHistory(TxDirectionType direction, string address, int take, string afterPagingToken)
        {
            string afterKey = null;
            if (afterPagingToken != null)
            {
                afterKey = TxHistory.GetKey(afterPagingToken, 999);
            }

            var memo = _balanceService.GetPublicAddressExtension(address);
            var result = await _txHistoryRepository.GetAllAfterHashAsync(direction, memo, take, afterKey);
            return result;
        }

        private async Task<List<TxHistory>> GetBaseAddressHistory(TxDirectionType direction, string address, int take, string afterPagingToken)
        {
            var result = new List<TxHistory>();
            var context = new TransactionContext
            {
                Cursor = afterPagingToken
            };

#pragma warning disable 1998
            async Task<bool> Process(TxDirectionType type, TxHistory history)
            {
                if (direction == type)
                {
                    result.Add(history);
                }

                return result.Count >= take;
            }
#pragma warning restore 1998

            do
            {
                await QueryAndProcessTransactions(address, context, Process);
            }
            while (!string.IsNullOrEmpty(context.Cursor) && result.Count < take);

            return result;
        }

        public async Task<int> UpdateDepositBaseTransactionHistory()
        {
            var count = 0;

            try
            {
                var depositBase = _balanceService.GetDepositBaseAddress();
                var context = new TransactionContext
                {
                    Cursor = await _keyValueStoreRepository.GetAsync(GetPagingTokenKey)
                };

                do
                {
                    count += await QueryAndProcessTransactions(depositBase, context, SaveTransactionHistory);

                    if (!string.IsNullOrEmpty(context.Cursor))
                    {
                        await _keyValueStoreRepository.SetAsync(GetPagingTokenKey, context.Cursor);
                    }
                }
                while (!string.IsNullOrEmpty(context.Cursor));

                _lastJobError = null;
            }
            catch (Exception ex)
            {
                _lastJobError = $"Error in job {nameof(TransactionHistoryService)}.{nameof(UpdateDepositBaseTransactionHistory)}: {ex.Message}";
                throw new JobExecutionException("Failed to execute deposit base transaction history update", ex, count);
            }

            return count;
        }

        private string GetPagingTokenKey => $"TransactionHistoryPagingToken:{_balanceService.GetDepositBaseAddress()}";

        private async Task<bool> SaveTransactionHistory(TxDirectionType direction, TxHistory history)
        {
            await _txHistoryRepository.InsertOrReplaceAsync(direction, history);
            return false;
        }

        public string GetLastJobError()
        {
            return _lastJobError;
        }

        private async Task<int> QueryAndProcessTransactions(string address, TransactionContext context, Func<TxDirectionType, TxHistory, Task<bool>> process)
        {
            var transactions = await _horizonService.GetTransactions(address, StellarSdkConstants.OrderAsc, context.Cursor);

            var count = 0;
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

                    for (short i = 0; i < tx.Operations.Length; i++)
                    {
                        var operation = tx.Operations[i];
                        var operationType = operation.Body.Discriminant.InnerValue;

                        var history = new TxHistory
                        {
                            FromAddress = transaction.SourceAccount,
                            AssetId = _blockchainAssetsService.GetNativeAsset().Id,
                            Hash = transaction.Hash,
                            OperationIndex = i,
                            PagingToken = transaction.PagingToken,
                            CreatedAt = transaction.CreatedAt,
                            Memo = _horizonService.GetMemo(transaction)
                        };

                        // ReSharper disable once SwitchStatementMissingSomeCases
                        switch (operationType)
                        {
                            case OperationType.OperationTypeEnum.CREATE_ACCOUNT:
                            {
                                var op = operation.Body.CreateAccountOp;
                                var keyPair = KeyPair.FromXdrPublicKey(op.Destination.InnerValue);
                                history.ToAddress = keyPair.Address;
                                history.Amount = op.StartingBalance.InnerValue;
                                history.PaymentType = PaymentType.CreateAccount;
                                break;
                            }
                            case OperationType.OperationTypeEnum.PAYMENT:
                            {
                                var op = operation.Body.PaymentOp;
                                if (op.Asset.Discriminant.InnerValue == AssetType.AssetTypeEnum.ASSET_TYPE_NATIVE)
                                {
                                    var keyPair = KeyPair.FromXdrPublicKey(op.Destination.InnerValue);
                                    history.ToAddress = keyPair.Address;
                                    history.Amount = op.Amount.InnerValue;
                                    history.PaymentType = PaymentType.Payment;
                                }
                                break;
                            }
                            case OperationType.OperationTypeEnum.ACCOUNT_MERGE:
                            {
                                var op = operation.Body;
                                var keyPair = KeyPair.FromXdrPublicKey(op.Destination.InnerValue);
                                history.ToAddress = keyPair.Address;
                                history.Amount = _horizonService.GetAccountMergeAmount(transaction.ResultXdr, i);
                                history.PaymentType = PaymentType.AccountMerge;
                                break;
                            }
                            case OperationType.OperationTypeEnum.PATH_PAYMENT:
                            {
                                var op = operation.Body.PathPaymentOp;
                                if (op.DestAsset.Discriminant.InnerValue == AssetType.AssetTypeEnum.ASSET_TYPE_NATIVE)
                                {
                                    var keyPair = KeyPair.FromXdrPublicKey(op.Destination.InnerValue);
                                    history.ToAddress = keyPair.Address;
                                    history.Amount = op.DestAmount.InnerValue;
                                    history.PaymentType = PaymentType.PathPayment;
                                }
                                break;
                            }
                            default:
                                continue;
                        }

                        if (!ForbiddenCharacterAzureStorageUtils.IsValidRowKey(history.Memo))
                        {
                            await _log.WriteErrorAsync(nameof(TransactionHistoryService),
                                nameof(QueryAndProcessTransactions),
                                history.Memo,
                                new Exception("Possible cashin skipped. It has forbiddden characters in memo."));

                            continue;
                        }

                        var cancel = false;
                        if (address.Equals(history.ToAddress, StringComparison.OrdinalIgnoreCase))
                        {
                            cancel = await process(TxDirectionType.Incoming, history);
                        }
                        if (address.Equals(history.FromAddress, StringComparison.OrdinalIgnoreCase))
                        {
                            cancel = await process(TxDirectionType.Outgoing, history);
                        }
                        if (cancel) return count;
                    }
                }
                catch (Exception ex)
                {
                    throw new BusinessException($"Failed to process transaction. hash={transaction?.Hash}", ex);
                }
            }

            return count;
        }
    }
}
