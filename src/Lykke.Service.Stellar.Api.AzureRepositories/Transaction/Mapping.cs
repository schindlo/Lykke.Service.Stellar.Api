using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    public static class Mapping
    {
        public static TxHistory ToDomain(this TxHistoryEntity entity)
        {
            var history = new TxHistory
            {
                OperationId = entity.OperationId,
                Timestamp = entity.Timestamp,
                FromAddress = entity.FromAddress,
                ToAddress = entity.ToAddress,
                AssetId = entity.AssetId,
                Amount = entity.Amount,
                Hash = entity.Hash,
                PaymentOperationId = entity.PaymentOperationId
            };
            return history;
        }
    }
}
