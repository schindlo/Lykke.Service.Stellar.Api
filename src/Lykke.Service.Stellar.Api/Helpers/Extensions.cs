using System;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.Helpers
{
    public static class Extensions
    {
        public static AssetResponse ToAssetResponse(this Asset self)
        {
            return new AssetResponse
            {
                Accuracy = self.Accuracy,
                Address = self.Address,
                AssetId = self.Id,
                Name = self.Name
            };
        }

        public static BroadcastedTransactionState ToBroadcastedTransactionState(this TxBroadcastState self)
        {
            switch (self)
            {
                case TxBroadcastState.InProgress:
                    return BroadcastedTransactionState.InProgress;
                case TxBroadcastState.Completed:
                    return BroadcastedTransactionState.Completed;
                case TxBroadcastState.Failed:
                    return BroadcastedTransactionState.Failed;
                default:
                    throw new ArgumentException("Failed to convert " +
                                                $"{nameof(TxBroadcastState)}.{Enum.GetName(typeof(TxBroadcastState), self)} " +
                                                $"to {nameof(BroadcastedTransactionState)}");
            }
        }

        public static BlockchainErrorCode? ToTransactionExecutionError(this TxExecutionError self)
        {
            switch (self)
            {
                case TxExecutionError.Unknown:
                    return BlockchainErrorCode.Unknown;
                case TxExecutionError.AmountIsTooSmall:
                    return BlockchainErrorCode.AmountIsTooSmall;
                case TxExecutionError.NotEnoughBalance:
                    return BlockchainErrorCode.NotEnoughBalance;
                case TxExecutionError.BuildingShouldBeRepeated:
                    return BlockchainErrorCode.BuildingShouldBeRepeated;
                default:
                    throw new ArgumentException("Failed to convert " +
                                                $"{nameof(TxExecutionError)}.{Enum.GetName(typeof(TxExecutionError), self)} " +
                                                $"to {nameof(BlockchainErrorCode)}");
            }
        }
    }
}
