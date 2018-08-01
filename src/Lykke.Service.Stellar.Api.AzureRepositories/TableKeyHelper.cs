using System;
using Common;

namespace Lykke.Service.Stellar.Api.AzureRepositories
{
    public static class TableKeyHelper
    {
        public static string GetRowKey(Guid operationId)
        {
            return operationId.ToString();
        }

        public static string GetHashedRowKey(string rowKey)
        {
            // Use hash to distribute all records to the different partitions
            var hash = rowKey.CalculateHexHash32(3);
            return $"{hash}";
        }
    }
}
