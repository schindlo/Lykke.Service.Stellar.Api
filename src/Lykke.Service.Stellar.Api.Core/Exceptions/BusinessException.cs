using System;

namespace Lykke.Service.Stellar.Api.Core.Exceptions
{
    public sealed class BusinessException : Exception
    {
        private const string ErrorCodeDataKey = "ErrorCode";

        public BusinessException(string message,
                                 string errorCode = null)
            : base(message)
        {
            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                Data.Add(ErrorCodeDataKey, errorCode);
            }
        }

        public BusinessException(string message,
                                 Exception inner,
                                 string errorCode = null)
            : base(message, inner)
        {
            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                Data.Add(ErrorCodeDataKey, errorCode);
            }
        }

        public string ErrorCode => Data.Contains(ErrorCodeDataKey) ? Data[ErrorCodeDataKey].ToString() : null;
    }
}
