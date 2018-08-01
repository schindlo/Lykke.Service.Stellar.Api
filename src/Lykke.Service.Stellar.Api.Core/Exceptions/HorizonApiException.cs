using System;

namespace Lykke.Service.Stellar.Api.Core.Exceptions
{
    public class HorizonApiException : Exception
    {
        public HorizonApiException(string message)
            : base(message)
        {
        }
    }
}
