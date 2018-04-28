using Lykke.Service.Stellar.Api.Core.Domain.Balance;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Balance
{
    public static class Mapping
    {
        public static WalletBalance ToDomain(this WalletBalanceEntity entity)
        {
            var balance = new WalletBalance
            {
                Address = entity.Address,
                AssetId = entity.AssetId,
                Balance = entity.Balance,
                Ledger = entity.Ledger,
                OperationIndex = entity.OperationIndex
            };
            return balance;
        }

        public static WalletBalanceEntity ToEntity(this WalletBalance domain)
        {
            var rowKey = WalletBalanceEntity.GetRowKey(domain.AssetId, domain.Address);
            var entity = new WalletBalanceEntity
            {
                PartitionKey = TableKey.GetHashedRowKey(rowKey),
                RowKey = rowKey,
                Balance = domain.Balance,
                Ledger = domain.Ledger,
                OperationIndex = domain.OperationIndex
            };
            return entity;
        }
    }
}
