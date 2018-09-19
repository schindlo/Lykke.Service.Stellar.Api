using System;

namespace Lykke.Service.Stellar.Api.Core.Domain
{
    public class Constants
    {
        public static Version ContractVersion = new Version("1.2.0");

            public class PublicAddressExtension
            {
                public const char Separator = '$';

                public const string DisplayName = "Memo (text)";
            }
    }
}
