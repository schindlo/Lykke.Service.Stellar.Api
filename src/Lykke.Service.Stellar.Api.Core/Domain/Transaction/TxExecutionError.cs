namespace Lykke.Service.Stellar.Api.Core.Domain.Transaction
{
    public enum TxExecutionError
    {
        Unknown = 0,
        AmountIsTooSmall = 1,
        NotEnoughBalance = 2,
        BuildingShouldBeRepeated = 3
    }
}
