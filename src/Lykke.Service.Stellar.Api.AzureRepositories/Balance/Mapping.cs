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
                Ledger = entity.Ledger
            };
            return balance;
        }
    }
}
