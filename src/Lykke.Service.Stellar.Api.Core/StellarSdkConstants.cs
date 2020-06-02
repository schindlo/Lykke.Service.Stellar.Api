namespace Lykke.Service.Stellar.Api.Core
{
    public static class StellarSdkConstants
    {
        public const string OrderAsc = "asc";

        public const string OrderDesc = "desc";

        public const string MemoTextTypeName = "text";

        public const string MemoIdTypeName = "id";

        public const string OperationUnderfunded = "op_underfunded";

        public const string OperationLowReserve = "op_low_reserve";

        public const byte MaxMemoLength = 28;
    }

    public class One
    {
        public static readonly int Value = 10000000;

        public static implicit operator double(One d)
        {
            return 1.0;
        }

        public static implicit operator long(One d)
        {
            return Value;
        }
    }
}
