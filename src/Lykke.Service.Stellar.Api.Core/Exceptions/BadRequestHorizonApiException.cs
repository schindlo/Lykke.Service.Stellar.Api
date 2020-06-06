using System;
using System.Collections.Generic;
using System.Linq;

namespace Lykke.Service.Stellar.Api.Core.Exceptions
{
    public class BadRequestHorizonApiException : Exception
    {
        public BadRequestHorizonApiException(string message, IReadOnlyCollection<string> errorCodes)
            : base(message)
        {
            ErrorCodes = errorCodes?.ToArray() ?? Array.Empty<string>();
        }

        public string[] ErrorCodes { get; }
    }
}
