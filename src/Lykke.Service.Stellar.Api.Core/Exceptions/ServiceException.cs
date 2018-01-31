using System;

namespace Lykke.Service.Stellar.Api.Core.Exceptions
{
    public class ServiceException: Exception
    {
        public ServiceException()
        {
        }

        public ServiceException(string message)
        : base(message)
        {
        }

        public ServiceException(string message, Exception inner)
        : base(message, inner)
        {
        }
    }
}
