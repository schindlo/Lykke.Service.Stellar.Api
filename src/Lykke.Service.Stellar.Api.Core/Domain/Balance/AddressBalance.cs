namespace Lykke.Service.Stellar.Api.Core.Domain.Balance
{
    public class AddressBalance
    {
        public string Address { get; set; }

        public long Sequence { get; set; }

        public long Balance { get; set; }

        public long MinBalance { get; set; }
    }
}
