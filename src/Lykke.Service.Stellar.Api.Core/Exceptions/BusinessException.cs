using System;

namespace Lykke.Service.Stellar.Api.Core.Exceptions
{
    public class BusinessException: Exception
    {
        public BusinessException()
        {
        }

        public BusinessException(string message)
        : base(message)
        {
        }

        public BusinessException(string message, Exception inner)
        : base(message, inner)
        {
        }
    }
}
