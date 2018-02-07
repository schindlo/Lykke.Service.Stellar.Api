namespace Lykke.Service.Stellar.Api.Services.Transaction
{
    internal class PaymentCursor
    {
        internal PaymentCursor()
        {
            Cursor = string.Empty;
            Sequence = 1;
        }

        internal string Cursor { get; set; }

        internal ulong Sequence { get; set; }
    }
}
