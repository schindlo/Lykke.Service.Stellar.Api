using System;

namespace Lykke.Service.Stellar.Api.Core.Exceptions
{
    public sealed class JobExecutionException : Exception
    {
        private const string ProcessedDataKey = "Processed";

        public JobExecutionException(string message,
                                     int processed)
            : base(message)
        {
            Data.Add(ProcessedDataKey, processed);
        }

        public JobExecutionException(string message,
                                     Exception inner,
                                     int processed)
            : base(message, inner)
        {
            Data.Add(ProcessedDataKey, processed);
        }

        public int Processed
        {
            get => Data.Contains(ProcessedDataKey) ? (int)Data[ProcessedDataKey] : 0;
        }
    }
}
