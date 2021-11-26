using Autofac;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Service.Stellar.Api.Core;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Tools.Erc20Exporter.Helpers;
using System;
using System.Numerics;
using System.Threading.Tasks;
using stellar_dotnet_sdk;
using stellar_dotnet_sdk.requests;
using stellar_dotnet_sdk.xdr;

namespace Lykke.Tools.Stellar.Commands
{
    public class BalanceCalculationCommand : ICommand
    {
        private readonly IConfigurationHelper _helper;
        private readonly string _horizonUrl;
        private readonly string _passPhrase;
        private BigInteger _amountSoFar = 0;
        private readonly string _address;
        private readonly BigInteger? _latestBlock;

        public BalanceCalculationCommand(IConfigurationHelper helper,
            string horizonUrl,
            string passPhrase,
            string address,
            BigInteger? latestBlock)
        {
            _helper = helper;
            _horizonUrl = horizonUrl;
            _passPhrase = passPhrase;
            _address = address;
            _latestBlock = latestBlock;
        }

        public async Task<int> ExecuteAsync()
        {
            return await CalculateBalanceAsync();
        }

        private async Task<int> CalculateBalanceAsync()
        {
            #region RegisterDependencies

            var (resolver, consoleLogger) = _helper.GetResolver(_horizonUrl, _passPhrase);

            #endregion

            var horizonService = resolver.Resolve<IHorizonService>();

            consoleLogger.Info("Started calculating");

            var count = 0;

            try
            {
                string cursor = null;

                do
                {
                    var result = await ProcessTransactionsAsync(horizonService, consoleLogger, cursor);
                    count += result.Count;
                    cursor = result.Cursor;
                    consoleLogger.Info($"Processed transactions so far: {count}");
                }
                while (!string.IsNullOrEmpty(cursor));
            }
            catch (Exception ex)
            {
                consoleLogger.Error(ex);
            }

            consoleLogger.Info($"Balance so far: {_amountSoFar}");
            consoleLogger.Info("Completed!");

            return 0;
        }

        private async Task<(int Count, string Cursor)> ProcessTransactionsAsync(IHorizonService horizonService, ILog logger, string cursor)
        {
            var transactions = await horizonService.GetTransactions(_address, OrderDirection.ASC, cursor);
            var count = 0;
            cursor = null;
            foreach (var transaction in transactions)
            {
                try
                {
                    if (_latestBlock.HasValue && (long)transaction.Ledger > _latestBlock)
                    {
                        cursor = null;
                        break;
                    }

                    var sign = 1;
                    cursor = transaction.PagingToken;
                    count++;

                    // skip outgoing transactions and transactions without memo
                    //var memo = horizonService.GetMemo(transaction);
                    if (_address.Equals(transaction.SourceAccount, StringComparison.OrdinalIgnoreCase))
                    {
                        sign = -1;
                    }

                    var xdr = Convert.FromBase64String(transaction.EnvelopeXdr);
                    var reader = new XdrDataInputStream(xdr);
                    var txEnvelope = TransactionEnvelope.Decode(reader);
                    var tx = txEnvelope.V0.Tx.Operations;

                    for (short i = 0; i < tx.Length; i++)
                    {
                        var operation = tx[i];
                        var operationType = operation.Body.Discriminant.InnerValue;

                        string toAddress = null;
                        long amount = 0;
                        // ReSharper disable once SwitchStatementMissingSomeCases
                        switch (operationType)
                        {
                            case OperationType.OperationTypeEnum.PAYMENT:
                                {
                                    var op = operation.Body.PaymentOp;
                                    if (op.Asset.Discriminant.InnerValue == AssetType.AssetTypeEnum.ASSET_TYPE_NATIVE)
                                    {
                                        var keyPair = KeyPair.FromPublicKey(op.Destination.Ed25519.InnerValue);
                                        toAddress = keyPair.Address;
                                        amount = op.Amount.InnerValue;
                                    }
                                    break;
                                }
                            case OperationType.OperationTypeEnum.ACCOUNT_MERGE:
                                {
                                    var op = operation.Body;
                                    var keyPair = KeyPair.FromPublicKey(op.Destination.Ed25519.InnerValue);
                                    toAddress = keyPair.Address;
                                    amount = horizonService.GetAccountMergeAmount(transaction.ResultXdr, i);
                                    break;
                                }
                            case OperationType.OperationTypeEnum.PATH_PAYMENT_STRICT_RECEIVE:
                                {
                                    var op = operation.Body.PathPaymentStrictReceiveOp;
                                    if (op.DestAsset.Discriminant.InnerValue == AssetType.AssetTypeEnum.ASSET_TYPE_NATIVE)
                                    {
                                        var keyPair = KeyPair.FromPublicKey(op.Destination.Ed25519.InnerValue);
                                        toAddress = keyPair.Address;
                                        amount = op.DestAmount.InnerValue;
                                    }
                                    break;
                                }
                            case OperationType.OperationTypeEnum.CREATE_ACCOUNT:
                            {
                                var op = operation.Body.CreateAccountOp;
                                if (op != null)
                                {
                                    var keyPair = KeyPair.FromXdrPublicKey(op.Destination.InnerValue);
                                    toAddress = keyPair.Address;
                                    amount = op.StartingBalance.InnerValue;
                                }
                                break;
                            }
                            default:
                                continue;
                        }

                        //var addressWithExtension = $"{toAddress}{Constants.PublicAddressExtension.Separator}{memo.ToLower()}";
                        var amountChange = (sign * amount);
                        _amountSoFar += amountChange;
                       //logger.Info($"Balance changed to {_amountSoFar} ({amountChange}) on ledger {transaction.Ledger}");
                    }
                }
                catch (Exception ex)
                {
                    throw new BusinessException($"Failed to process transaction. hash={transaction?.Hash}", ex);
                }
            }

            return (count, cursor);
        }
    }
}
