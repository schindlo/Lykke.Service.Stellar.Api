using StellarSdk.Model;

namespace Lykke.Service.Stellar.Api.Services.Transaction
{
    internal class PaymentContext
    {
        internal PaymentContext()
        {
            Cursor = string.Empty;
            Sequence = 1;
        }

        internal string Cursor { get; set; }

        internal ulong Sequence { get; set; }

        internal TransactionDetails Transaction { get; set; }

        internal int AccountMerge { get; set; }
    }
}
