using System;
using Lykke.AzureStorage.Tables;
using Lykke.AzureStorage.Tables.Entity.Annotation;
using Lykke.AzureStorage.Tables.Entity.ValueTypesMerging;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.AzureRepositories.Transaction
{
    [ValueTypeMergingStrategy(ValueTypeMergingStrategy.UpdateIfDirty)]
    public class TxBroadcastEntity : AzureTableEntity
    {
        private TxBroadcastState _state;
        private long _amount;
        private long _fee;
        private long _ledger;
        private DateTime _createdAt;
        private string _error;
        private TxExecutionError? _errorCode;

        public Guid OperationId => Guid.Parse(RowKey);

        public TxBroadcastState State 
        { 
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    MarkValueTypePropertyAsDirty(nameof(State));
                }
            }
        }

        public long Amount
        {
            get => _amount;
            set
            {
                if (_amount != value)
                {
                    _amount = value;
                    MarkValueTypePropertyAsDirty(nameof(Amount));
                }
            }
        }

        public long Fee
        {
            get => _fee;
            set
            {
                if (_fee != value)
                {
                    _fee = value;
                    MarkValueTypePropertyAsDirty(nameof(Fee));
                }
            }
        }

        public string Hash { get; set; }

        public long Ledger
        { 
            get => _ledger;
            set
            {
                if (_ledger != value)
                {
                    _ledger = value;
                    MarkValueTypePropertyAsDirty(nameof(Ledger));
                }
            }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                if (_createdAt != value)
                {
                    _createdAt = value;
                    MarkValueTypePropertyAsDirty(nameof(CreatedAt));
                }
            }
        }

        public string Error
        {
            get => _error;
            set
            {
                if (_error != value)
                {
                    _error = value;
                    MarkValueTypePropertyAsDirty(nameof(Error));
                }
            }
        }

        public TxExecutionError? ErrorCode
        { 
            get => _errorCode;
            set
            {
                if (_errorCode != value)
                {
                    _errorCode = value;
                    MarkValueTypePropertyAsDirty(nameof(ErrorCode));
                }
            }
        }
    }
}
