namespace Lykke.Service.Stellar.Api.Services.Transaction
{
    internal class TransactionContext
    {
        internal TransactionContext()
        {
            Cursor = string.Empty;
        }

        internal string Cursor { get; set; }
    }
}
