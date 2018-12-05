using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Balance
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateIfDirty)]
    public class WalletBalanceEntity : AzureTableEntity
    {
        private long _balance;
        private long _ledger;

        public string AssetId => RowKey.Split(':')[0];

        public string Address => RowKey.Split(':')[1];

        public long Balance 
        { 
            get => _balance;
            set
            {
                if (_balance == value) return;
                _balance = value;
                MarkValueTypePropertyAsDirty(nameof(Balance));
            }
        }

        public long Ledger
        {
            get => _ledger;
            set
            {
                if (_ledger == value) return;
                _ledger = value;
                MarkValueTypePropertyAsDirty(nameof(Ledger));
            }
        }

        public static string GetRowKey(string assetId, string address)
        {
            return $"{assetId}:{address}";
        }
    }
}
