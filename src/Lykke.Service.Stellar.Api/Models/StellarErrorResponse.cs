using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lykke.Service.BlockchainApi.Contract;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lykke.Service.Stellar.Api.Models
{
    public class StellarErrorResponse
    {
        public string ErrorMessage { get; }

        public BlockchainErrorCode ErrorCode { get; }

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

        public StellarErrorResponse AddModelError(string key, string message)
        {
            if (!ModelErrors.TryGetValue(key, out List<string> errors))
            {
                errors = new List<string>();

                ModelErrors.Add(key, errors);
            }

            errors.Add(message);

            return this;
        }

        public StellarErrorResponse AddModelError(string key, Exception exception)
        {
            var ex = exception;
            var sb = new StringBuilder();

            while (true)
            {
                sb.AppendLine(ex.Message);

                ex = ex.InnerException;

                if (ex == null)
                {
                    return AddModelError(key, sb.ToString());
                }

                sb.Append(" -> ");
            }
        }

        public static StellarErrorResponse Create()
        {
            return new StellarErrorResponse();
        }

        public static StellarErrorResponse Create(ModelStateDictionary modelState)
        {
            var response = new StellarErrorResponse();

            foreach (var state in modelState)
            {
                var messages = state.Value.Errors
                    .Where(e => !string.IsNullOrWhiteSpace(e.ErrorMessage))
                    .Select(e => e.ErrorMessage)
                    .Concat(state.Value.Errors
                        .Where(e => string.IsNullOrWhiteSpace(e.ErrorMessage))
                        .Select(e => e.Exception.Message))
                    .ToList();

                response.ModelErrors.Add(state.Key, messages);
            }

            return response;
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