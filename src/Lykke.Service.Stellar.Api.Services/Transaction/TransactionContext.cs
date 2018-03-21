namespace Lykke.Service.Stellar.Api.Services.Transaction
{
    internal class TransactionContext
    {
        internal TransactionContext(string tableId)
        {
            Cursor = string.Empty;
            TableId = tableId;
        }

        internal string Cursor { get; set; }

        internal string TableId { get; set; }
    }
}
