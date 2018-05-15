using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Lykke.Service.BlockchainApi.Contract;

namespace Lykke.Service.Stellar.Api.Models
{
    public class StellarErrorResponse
    {
        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; }

        [JsonProperty("errorCode")]
        [JsonConverter(typeof(StringEnumConverter))]
        public BlockchainErrorCode ErrorCode { get; }

        [JsonProperty("modelErrors")]
        public Dictionary<string, List<string>> ModelErrors { get; }

        private StellarErrorResponse() :
        this(null, BlockchainErrorCode.Unknown)
        {
        }

        private StellarErrorResponse(string errorMessage, BlockchainErrorCode errorCode)
        {
            ErrorMessage = errorMessage;
            ErrorCode = errorCode;
            ModelErrors = new Dictionary<string, List<string>>();
        }

        public static StellarErrorResponse Create(string message)
        {
            return new StellarErrorResponse(message, BlockchainErrorCode.Unknown);
        }

        public static StellarErrorResponse Create(string message, BlockchainErrorCode errorCode)
        {
            return new StellarErrorResponse(message, errorCode);
        }
    }
}
