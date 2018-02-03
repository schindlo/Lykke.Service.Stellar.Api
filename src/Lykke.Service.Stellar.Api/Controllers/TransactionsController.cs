using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Helpers;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.Stellar.Api.Core.Domain;

namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("api/transactions")]
    public class TransactionsController : Controller
    {
        private readonly IStellarService _stellarService;

        public TransactionsController(IStellarService stellarService)
        {
            _stellarService = stellarService;
        }

        [HttpPost("single")]
        [ProducesResponseType(typeof(BuildTransactionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> BuildSingle([Required, FromBody] BuildSingleTransactionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            string xdrBase64;
            var build = await _stellarService.GetTxBuildAsync(request.OperationId);
            if (build != null)
            {
                xdrBase64 = build.XdrBase64;
            }
            else
            {
                if (!_stellarService.IsAddressValid(request.FromAddress))
                {
                    return BadRequest(ErrorResponse.Create($"{nameof(request.FromAddress)} is not a valid"));
                }

                if (!_stellarService.IsAddressValid(request.ToAddress))
                {
                    return BadRequest(ErrorResponse.Create($"{nameof(request.ToAddress)} is not a valid"));
                }

                if (request.AssetId != Asset.Stellar.Id)
                {
                    return BadRequest(ErrorResponse.Create($"{nameof(request.AssetId)} was not found"));
                }

                var amount = Int64.Parse(request.Amount);
                var fees = await _stellarService.GetFeesAsync();
                var fromAddressBalance = await _stellarService.GetAddressBalanceAsync(request.FromAddress, fees);

                var requiredBalance = request.IncludeFee ? amount : amount + fees.BaseFee;
                if (requiredBalance >= fromAddressBalance)
                {
                    return StatusCode(StatusCodes.Status406NotAcceptable,
                        ErrorResponse.Create($"There no enough funds on {nameof(request.FromAddress)} (" +
                                             $"required: {requiredBalance}, available: {fromAddressBalance})"));
                }

                xdrBase64 = await _stellarService.BuildTransactionAsync(request.OperationId, request.FromAddress, request.ToAddress, amount);
            }

            return Ok(new BuildTransactionResponse
            {
                TransactionContext = xdrBase64
            });
        }

        [HttpPost("broadcast")]
        public async Task<IActionResult> Broadcast([Required, FromBody] BroadcastTransactionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var broadcast = await _stellarService.GetTxBroadcastAsync(request.OperationId);
            if (broadcast != null)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }

            await _stellarService.BroadcastTxAsync(request.OperationId, request.SignedTransaction);

            return Ok();
        }

        [HttpDelete("broadcast/{operationId}")]
        public async Task<IActionResult> DeleteBroadcast([Required] Guid operationId)
        {
            var broadcast = await _stellarService.GetTxBroadcastAsync(operationId);
            if (broadcast == null)
            {
                return new StatusCodeResult(StatusCodes.Status204NoContent);
            }

            await _stellarService.DeleteTxBroadcastAsync(operationId);

            return Ok();
        }

        [HttpGet("broadcast/single/{operationId}")]
        [ProducesResponseType(typeof(BroadcastedSingleTransactionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBroadcastSingle([Required] Guid operationId)
        {
            var broadcast = await _stellarService.GetTxBroadcastAsync(operationId);
            if (broadcast == null)
            {
                return new StatusCodeResult(StatusCodes.Status204NoContent);
            }

            return Ok(new BroadcastedSingleTransactionResponse
            {
                OperationId = broadcast.OperationId,
                State = broadcast.State.ToBroadcastedTransactionState(),
                Timestamp = broadcast.Timestamp.UtcDateTime,
                Amount = broadcast.Amount.ToString(),
                Fee = broadcast.Fee.ToString(),
                Hash = broadcast.Hash,
                Block = broadcast.Ledger ?? 0,
                Error = broadcast.Error,
                ErrorCode = broadcast.ErrorCode.HasValue ? broadcast.ErrorCode.Value.ToTransactionExecutionError() : null
            });
        }

        [HttpPost("many-inputs")]
        public IActionResult BuildWithManyInputs([Required, FromBody] BuildTransactionWithManyInputsRequest request)
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }

        [HttpPost("many-outputs")]
        public IActionResult BuildWithManyOutputs([Required, FromBody] BuildTransactionWithManyOutputsRequest request)
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }

        [HttpPost]
        [ProducesResponseType(typeof(RebuildTransactionResponse), StatusCodes.Status501NotImplemented)]
        public IActionResult Rebuild([Required, FromBody] RebuildTransactionRequest request)
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }

        [HttpGet("broadcast/many-inputs/{operationId}")]
        [ProducesResponseType(typeof(BroadcastedTransactionWithManyInputsResponse), StatusCodes.Status501NotImplemented)]
        public IActionResult GetBroadcastWithManyInputs([Required] Guid operationId)
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }

        [HttpGet("broadcast/many-outputs/{operationId}")]
        [ProducesResponseType(typeof(BroadcastedTransactionWithManyOutputsResponse), StatusCodes.Status501NotImplemented)]
        public IActionResult GetBroadcastWithManyOutputs([Required] Guid operationId)
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }
    }
}
