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
        private int _operationIndex;
        private string _lastTransactionHash;

        public string AssetId
        {
            get => RowKey.Split(':')[0];
        }

        public string Address
        {
            get => RowKey.Split(':')[1];
        }

        public long Balance 
        { 
            get
            {
                return _balance;
            }
            set
            {
                if (_balance != value)
                {
                    _balance = value;
                    MarkValueTypePropertyAsDirty(nameof(Balance));
                }
            }
        }

        public long Ledger
        {
            get
            {
                return _ledger;
            }
            set
            {
                if (_ledger != value)
                {
                    _ledger = value;
                    MarkValueTypePropertyAsDirty(nameof(Ledger));
                }
            }
        }

        public int OperationIndex
        {
            get
            {
                return _operationIndex;
            }
            set
            {
                if (_operationIndex != value)
                {
                    _operationIndex = value;
                    MarkValueTypePropertyAsDirty(nameof(OperationIndex));
                }
            }
        }

        public string LastTransactionHash
        {
            get
            {
                return _lastTransactionHash;
            }
            set
            {
                if (_lastTransactionHash != value)
                {
                    _lastTransactionHash = value;
                    MarkValueTypePropertyAsDirty(nameof(LastTransactionHash));
                }
            }
        }

        public static string GetRowKey(string assetId, string address)
        {
            return $"{assetId}:{address}";
        }
    }
}