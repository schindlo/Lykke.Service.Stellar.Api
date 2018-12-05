using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Balance
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateIfDirty)]
    public class WalletBalanceJournalEntity : AzureTableEntity
    {
        private string _assetId;
        private string _address;
        private long _ledger;
        private string _transactionHash;
        private long _operationId;
        private long _amount;

        public string AssetId
        {
            get => _assetId;
            set
            {
                if (_assetId == value) return;
                _assetId = value;
                MarkValueTypePropertyAsDirty(nameof(AssetId));
            }
        }

        public string Address
        {
            get => _address;
            set
            {
                if (_address == value) return;
                _address = value;
                MarkValueTypePropertyAsDirty(nameof(Address));
            }
        }

        public long Amount 
        { 
            get => _amount;
            set
            {
                if (_amount == value) return;
                _amount = value;
                MarkValueTypePropertyAsDirty(nameof(Amount));
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

        public long OperationId
        {
            get => _operationId;
            set
            {
                if (_operationId == value) return;
                _operationId = value;
                MarkValueTypePropertyAsDirty(nameof(OperationId));
            }
        }

        public string TransactionHash
        {
            get => _transactionHash;
            set
            {
                if (_transactionHash == value) return;
                _transactionHash = value;
                MarkValueTypePropertyAsDirty(nameof(TransactionHash));
            }
        }

        public static string GetPartitionKey(string assetId, string address)
        {
            return $"{assetId}_{address}";
        }

        public static string GetRowKey(string hash, long operationId)
        {
            return $"{hash}_{operationId}";
        }
    }
}
