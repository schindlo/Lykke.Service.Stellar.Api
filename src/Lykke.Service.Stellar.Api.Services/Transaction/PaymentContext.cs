using StellarSdk.Model;

namespace Lykke.Service.Stellar.Api.Services.Transaction
{
    internal class PaymentContext
    {
        internal PaymentContext(string tableId)
        {
            Cursor = string.Empty;
            Sequence = 1;
            TableId = tableId;
        }

        internal string Cursor { get; set; }

        internal ulong Sequence { get; set; }

        internal TransactionDetails Transaction { get; set; }

        internal int AccountMerge { get; set; }

        internal string TableId { get; set; }
    }
}
