using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common;
using JetBrains.Annotations;
using Lykke.Common.Log;
using StellarBase;
using StellarBase.Generated;
using StellarSdk.Exceptions;
using Lykke.Service.Stellar.Api.Core;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Observation;
using Newtonsoft.Json;
using Common.Log;
using Lykke.Common.Chaos;

namespace Lykke.Service.Stellar.Api.Services.Transaction
{
    public class TransactionService : ITransactionService
    {
        private string _lastJobError;

        private readonly IBalanceService _balanceService;
        private readonly IHorizonService _horizonService;
        private readonly IObservationRepository<BroadcastObservation> _observationRepository;
        private readonly IWalletBalanceRepository _balanceRepository;
        private readonly ITxBroadcastRepository _broadcastRepository;
        private readonly ITxBuildRepository _buildRepository;
        private readonly TimeSpan _transactionExpirationTime;
        private readonly ILog _log;
        private readonly IBlockchainAssetsService _blockchainAssetsService;
        private readonly IChaosKitty _chaos;

        [UsedImplicitly]
        public TransactionService(IBalanceService balanceService,
                                  IHorizonService horizonService,
                                  IObservationRepository<BroadcastObservation> observationRepository,
                                  IWalletBalanceRepository balanceRepository,
                                  ITxBroadcastRepository broadcastRepository,
                                  ITxBuildRepository buildRepository,
                                  TimeSpan transactionExpirationTime,
                                  ILogFactory logFactory,
                                  IBlockchainAssetsService blockchainAssetsService,
                                  IChaosKitty chaosKitty)
        {
            _balanceService = balanceService;
            _horizonService = horizonService;
            _observationRepository = observationRepository;
            _balanceRepository = balanceRepository;
            _broadcastRepository = broadcastRepository;
            _buildRepository = buildRepository;
            _transactionExpirationTime = transactionExpirationTime;
            _log = logFactory.CreateLog(this);
            _blockchainAssetsService = blockchainAssetsService;
            _chaos = chaosKitty;
        }

        public bool CheckSignature(string xdrBase64)
        {
            bool isSignOk = true;

            try
            {
                var xdr = Convert.FromBase64String(xdrBase64);
                var reader = new ByteReader(xdr);
                var txEnvelope = TransactionEnvelope.Decode(reader);
            }
            catch (Exception e)
            {
                isSignOk = false;
            }

            return isSignOk;
        }

        public async Task<TxBroadcast> GetTxBroadcastAsync(Guid operationId)
        {
            return await _broadcastRepository.GetAsync(operationId);
        }

        public async Task BroadcastTxAsync(Guid operationId, string xdrBase64, TxBroadcast broadcast = null)
        {
            long amount = 0;
            var xdr = Convert.FromBase64String(xdrBase64);
            var reader = new ByteReader(xdr);
            var txEnvelope = TransactionEnvelope.Decode(reader);

            _chaos.Meow(nameof(BroadcastTxAsync));

            if (!await ProcessDwToHwTransaction(operationId, txEnvelope.Tx, broadcast))
            {
                var operation = _horizonService.GetFirstOperationFromTxEnvelope(txEnvelope);
                var operationType = operation.Discriminant.InnerValue;

                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (operationType)
                {
                    case OperationType.OperationTypeEnum.CREATE_ACCOUNT:
                        {
                            amount = operation.CreateAccountOp.StartingBalance.InnerValue;
                            break;
                        }
                    case OperationType.OperationTypeEnum.PAYMENT:
                        {
                            amount = operation.PaymentOp.Amount.InnerValue;
                            break;
                        }
                    case OperationType.OperationTypeEnum.ACCOUNT_MERGE:
                        {
                            // amount not yet known
                            break;
                        }
                    default:
                        throw new BusinessException($"Unsupported operation type. type={operationType}");
                }

                string hash;

                try
                {
                    hash = await _horizonService.SubmitTransactionAsync(xdrBase64);
                }
                catch (Exception ex)
                {
                    broadcast = new TxBroadcast
                    {
                        OperationId = operationId,
                        State = TxBroadcastState.Failed,
                        Amount = amount,
                        CreatedAt = DateTime.UtcNow,
                        Error = GetErrorMessage(ex),
                        ErrorCode = GetErrorCode(ex)
                    };
                    await _broadcastRepository.InsertOrReplaceAsync(broadcast);

                    _log.Error(ex, message: "Broadcasting has failed!", context: new { OperationId = operationId });
                    throw new BusinessException($"Broadcasting transaction failed. operationId={operationId}, message={broadcast.Error}", ex, broadcast.ErrorCode.ToString());
                }

                _chaos.Meow(nameof(BroadcastTxAsync));

                var observation = new BroadcastObservation
                {
                    OperationId = operationId
                };

                await _observationRepository.InsertOrReplaceAsync(observation);

                _chaos.Meow(nameof(BroadcastTxAsync));

                broadcast = new TxBroadcast
                {
                    OperationId = operationId,
                    State = TxBroadcastState.InProgress,
                    Amount = amount,
                    Hash = hash,
                    CreatedAt = DateTime.UtcNow
                };

                await _broadcastRepository.InsertOrReplaceAsync(broadcast);
            }
        }

        private async Task<bool> ProcessDwToHwTransaction(Guid operationId, StellarBase.Generated.Transaction tx, TxBroadcast broadcast = null)
        {
            var fromKeyPair = KeyPair.FromXdrPublicKey(tx.SourceAccount.InnerValue);
            if (!_balanceService.IsDepositBaseAddress(fromKeyPair.Address) || tx.Operations.Length != 1 ||
                tx.Operations[0].Body.PaymentOp == null || string.IsNullOrWhiteSpace(tx.Memo.Text)) return false;

            var toKeyPair = KeyPair.FromXdrPublicKey(tx.Operations[0].Body.PaymentOp.Destination.InnerValue);
            if (!_balanceService.IsDepositBaseAddress(toKeyPair.Address)) return false;

            _chaos.Meow(nameof(ProcessDwToHwTransaction));

            var fromAddress = $"{fromKeyPair.Address}{Constants.PublicAddressExtension.Separator}{tx.Memo.Text}";
            var amount = tx.Operations[0].Body.PaymentOp.Amount.InnerValue;
            var hash = broadcast?.Hash ?? "ut_" + (DateTime.UtcNow.ToUnixTime()).ToString(CultureInfo.InvariantCulture);//_horizonService.GetTransactionHash(tx);
            var ledger = await _horizonService.GetLatestLedger();
            var updateLedger = (ledger.Sequence * 10) + 1;

            broadcast = new TxBroadcast
            {
                OperationId = operationId,
                Amount = amount,
                Fee = 0,
                Hash = hash,
                // ReSharper disable once ArrangeRedundantParentheses
                Ledger = updateLedger,
                CreatedAt = DateTime.UtcNow
            };

            // save without state to prevent changing of tx hash
            await _broadcastRepository.InsertOrReplaceAsync(broadcast);

            _chaos.Meow(nameof(ProcessDwToHwTransaction));

            var assetId = _blockchainAssetsService.GetNativeAsset().Id;
            var balance = await _balanceRepository.GetAsync(assetId, fromAddress);

            if (balance.Balance < amount)
            {
                broadcast.State = TxBroadcastState.Failed;
                broadcast.Error = "Not enough balance!";
                broadcast.ErrorCode = TxExecutionError.NotEnoughBalance;
            }
            else
            {
                await _balanceRepository.RecordOperationAsync(assetId, fromAddress, updateLedger, 0, hash, (-1) * amount);
                await _balanceRepository.RefreshBalance(assetId, fromAddress);
                broadcast.State = TxBroadcastState.Completed;
            }

            _chaos.Meow(nameof(ProcessDwToHwTransaction));

            // update state
            await _broadcastRepository.InsertOrReplaceAsync(broadcast);
            return true;
        }

        private static string GetErrorMessage(Exception ex)
        {
            var errorMessage = ex.Message;
            // ReSharper disable once InvertIf
            if (ex is BadRequestException badRequest)
            {
                var resultCodes = JsonConvert.SerializeObject(badRequest.ErrorDetails.Extras.ResultCodes);
                errorMessage += $". ResultCodes={resultCodes}";
            }
            return errorMessage;
        }

        private static TxExecutionError GetErrorCode(Exception ex)
        {
            var bre = ex as BadRequestException;
            var resultCodes = bre?.ErrorDetails?.Extras?.ResultCodes;
            var ops = resultCodes?.Operations;
            var transactionDetail = resultCodes?.Transaction;
            if (bre?.ErrorDetails != null && bre.ErrorDetails.Status == (int)HttpStatusCode.BadRequest
                && ops != null
                && ops.Length > 0
                && (ops[0].Equals(StellarSdkConstants.OperationUnderfunded)
                    || ops[0].Equals(StellarSdkConstants.OperationLowReserve)))
            {
                return TxExecutionError.NotEnoughBalance;
            }

            if (transactionDetail == "tx_too_late" ||
                transactionDetail == "tx_bad_seq")
            {
                return TxExecutionError.BuildingShouldBeRepeated;
            }

            return TxExecutionError.Unknown;
        }

        public async Task DeleteTxBroadcastAsync(Guid operationId)
        {
            var deleteObservationTask = _observationRepository.DeleteIfExistAsync(operationId.ToString());
            var deleteBroadcastTask = _broadcastRepository.DeleteAsync(operationId);
            await Task.WhenAll(deleteObservationTask, deleteBroadcastTask);
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

        public async Task<string> BuildTransactionAsync(Guid operationId, AddressBalance from, string toAddress, string memoText, long amount)
        {
            var fromKeyPair = KeyPair.FromAddress(from.Address);
            var fromAccount = new Account(fromKeyPair, from.Sequence);

            var toKeyPair = KeyPair.FromAddress(toAddress);

            var transferableBalance = from.Balance - from.MinBalance;

            StellarBase.Operation operation;
            if (await _horizonService.AccountExists(toAddress))
            {
                if (amount <= transferableBalance)
                {
                    var asset = new StellarBase.Asset();
                    operation = new PaymentOperation.Builder(toKeyPair, asset, amount)
                                                    .SetSourceAccount(fromKeyPair)
                                                    .Build();
                }
                else if (!_balanceService.IsDepositBaseAddress(from.Address))
                {
                    operation = new AccountMergeOperation.Builder(toKeyPair)
                                                         .SetSourceAccount(fromKeyPair)
                                                         .Build();
                }
                else
                {
                    throw new BusinessException($"It isn't allowed to merge the entire balance from the deposit base into another account! Transfer less funds. transferable={transferableBalance}");
                }
            }
            else
            {
                if (amount <= transferableBalance)
                {
                    operation = new CreateAccountOperation.Builder(toKeyPair, amount)
                                                      .SetSourceAccount(fromKeyPair)
                                                      .Build();
                }
                else
                {
                    throw new BusinessException($"It isn't possible to merge the entire balance into an unused account! Use a destination in existance. transferable={transferableBalance}");
                }
            }

            fromAccount.IncrementSequenceNumber();

            var builder = new StellarBase.Transaction.Builder(fromAccount)
                                         .AddOperation(operation);
            if (!string.IsNullOrWhiteSpace(memoText))
            {
                var memo = StellarBase.Memo.MemoText(memoText);
                builder = builder.AddMemo(memo);
            }

            var tx = builder.Build();

            var xdr = tx.ToXDR();
            var expirationDate = (DateTime.UtcNow + _transactionExpirationTime);
            var maxUnixTimeDouble = expirationDate.ToUnixTime() / 1000;//ms to seconds
            var maxTimeUnix = (ulong)maxUnixTimeDouble;
            xdr.TimeBounds = new TimeBounds()
            {
                MaxTime = new StellarBase.Generated.Uint64(maxTimeUnix),
                MinTime = new StellarBase.Generated.Uint64(0),
            };

            var writer = new ByteWriter();
            StellarBase.Generated.Transaction.Encode(writer, xdr);
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

        public async Task<int> UpdateBroadcastsInProgress(int batchSize)
        {
            var count = 0;

            try
            {
                string continuationToken = null;
                do
                {
                    var observations = await _observationRepository.GetAllAsync(batchSize, continuationToken);
                    foreach (var item in observations.Items)
                    {
                        await ProcessBroadcastInProgress(item.OperationId);
                        count++;
                    }
                    continuationToken = observations.ContinuationToken;
                } while (continuationToken != null);

                _lastJobError = null;
            }
            catch (Exception ex)
            {
                _lastJobError = $"Error in job {nameof(TransactionService)}.{nameof(UpdateBroadcastsInProgress)}: {ex.Message}";
                throw new JobExecutionException("Failed to execute broadcast in progress updates", ex, count);
            }

            return count;
        }

        private async Task ProcessBroadcastInProgress(Guid operationId)
        {
            TxBroadcast broadcast = null;
            try
            {
                broadcast = await _broadcastRepository.GetAsync(operationId);
                if (broadcast == null)
                {
                    await _observationRepository.DeleteIfExistAsync(operationId.ToString());
                    throw new BusinessException($"Broadcast for observed operation not found. operationId={operationId}");
                }

                var tx = await _horizonService.GetTransactionDetails(broadcast.Hash);
                if (tx == null)
                {
                    // transaction still in progress
                    return;
                }
                if (!broadcast.Hash.Equals(tx.Hash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessException($"Transaction hash mismatch. actual={tx.Hash}, expected={broadcast.Hash}");
                }

                var operation = _horizonService.GetFirstOperationFromTxEnvelopeXdr(tx.EnvelopeXdr);
                var operationType = operation.Discriminant.InnerValue;

                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (operationType)
                {
                    case OperationType.OperationTypeEnum.CREATE_ACCOUNT:
                        {
                            broadcast.Amount = operation.CreateAccountOp.StartingBalance.InnerValue;
                            break;
                        }
                    case OperationType.OperationTypeEnum.PAYMENT:
                        {
                            broadcast.Amount = operation.PaymentOp.Amount.InnerValue;
                            break;
                        }
                    case OperationType.OperationTypeEnum.ACCOUNT_MERGE:
                        {
                            broadcast.Amount = _horizonService.GetAccountMergeAmount(tx.ResultXdr, 0);
                            break;
                        }
                    default:
                        throw new BusinessException($"Unsupported operation type. type={operationType}");
                }

                broadcast.State = TxBroadcastState.Completed;
                broadcast.Fee = tx.FeePaid;
                broadcast.CreatedAt = tx.CreatedAt;
                broadcast.Ledger = tx.Ledger * 10;

                await _broadcastRepository.MergeAsync(broadcast);
                await _observationRepository.DeleteIfExistAsync(operationId.ToString());
            }
            catch (Exception ex)
            {
                if (broadcast != null)
                {
                    broadcast.State = TxBroadcastState.Failed;
                    broadcast.Error = ex.Message;
                    broadcast.ErrorCode = TxExecutionError.Unknown;

                    await _broadcastRepository.MergeAsync(broadcast);
                    await _observationRepository.DeleteIfExistAsync(operationId.ToString());
                }

                throw new BusinessException($"Failed to process in progress broadcast. operationId={operationId}", ex);
            }
        }
    }
}
