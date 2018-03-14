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
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.Stellar.Api.Models;

namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("api/transactions")]
    public class TransactionsController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly IBalanceService _balanceService;

        public TransactionsController(ITransactionService transactionService, IBalanceService balanceService)
        {
            _transactionService = transactionService;
            _balanceService = balanceService;
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
            var build = await _transactionService.GetTxBuildAsync(request.OperationId);
            if (build != null)
            {
                xdrBase64 = build.XdrBase64;
            }
            else
            {
                if (!_balanceService.IsAddressValid(request.FromAddress))
                {
                    return BadRequest(ErrorResponse.Create($"{nameof(request.FromAddress)} is not a valid"));
                }

                if (!_balanceService.IsAddressValid(request.ToAddress))
                {
                    return BadRequest(ErrorResponse.Create($"{nameof(request.ToAddress)} is not a valid"));
                }

                if (request.AssetId != Asset.Stellar.Id)
                {
                    return BadRequest(ErrorResponse.Create($"{nameof(request.AssetId)} was not found"));
                }

                Int64 amount;
                try
                {
                    amount = Int64.Parse(request.Amount);
                }
                catch (FormatException)
                {
                    // too small (e.g. 0.1)
                    return BadRequest(StellarErrorResponse.Create($"Amount is too small. min=1, amount={request.Amount}", BlockchainErrorCode.AmountIsTooSmall));
                }
                var fees = await _transactionService.GetFeesAsync();
                var fromAddressBalance = await _balanceService.GetAddressBalanceAsync(request.FromAddress, fees);
                var requiredBalance = request.IncludeFee ? amount : amount + fees.BaseFee;
                var availableBalance = fromAddressBalance.Balance - fromAddressBalance.MinBalance;
                if (requiredBalance >= availableBalance)
                {
                    return BadRequest(StellarErrorResponse.Create($"Not enough balance to create transaction. required={requiredBalance}, available={availableBalance}"
                                                                  , BlockchainErrorCode.NotEnoughtBalance));
                }

                xdrBase64 = await _transactionService.BuildTransactionAsync(request.OperationId, fromAddressBalance, request.ToAddress, amount);
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

            var broadcast = await _transactionService.GetTxBroadcastAsync(request.OperationId);
            if (broadcast != null)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }
            try
            {
                await _transactionService.BroadcastTxAsync(request.OperationId, request.SignedTransaction);
            }
            catch (BusinessException ex)
            {
                if (ex.Data.Contains("ErrorCode"))
                {
                    return BadRequest(StellarErrorResponse.Create(ex.Message, (BlockchainErrorCode)ex.Data["ErrorCode"]));
                }
                // technical / unknown problem
                throw ex;
            }

            return Ok();
        }

        [HttpDelete("broadcast/{operationId}")]
        public async Task<IActionResult> DeleteBroadcast([Required] Guid operationId)
        {
            var broadcast = await _transactionService.GetTxBroadcastAsync(operationId);
            if (broadcast == null)
            {
                return new StatusCodeResult(StatusCodes.Status204NoContent);
            }

            await _transactionService.DeleteTxBroadcastAsync(operationId);

            return Ok();
        }

        [HttpGet("broadcast/single/{operationId}")]
        [ProducesResponseType(typeof(BroadcastedSingleTransactionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBroadcastSingle([Required] Guid operationId)
        {
            if (Guid.Empty.Equals(operationId))
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError("operationId", "OperationId must be valid guid"));
            }
            var broadcast = await _transactionService.GetTxBroadcastAsync(operationId);
            if (broadcast == null)
            {
                return new StatusCodeResult(StatusCodes.Status204NoContent);
            }

            return Ok(new BroadcastedSingleTransactionResponse
            {
                OperationId = broadcast.OperationId,
                State = broadcast.State.ToBroadcastedTransactionState(),
                Timestamp = broadcast.CreatedAt.HasValue ? broadcast.CreatedAt.Value : DateTime.MinValue,
                Amount = broadcast.Amount.ToString(),
                Fee = broadcast.Fee.ToString(),
                Hash = broadcast.Hash,
                Block = broadcast.Ledger ?? 0,
                Error = broadcast.Error,
                ErrorCode = broadcast.ErrorCode.HasValue ? broadcast.ErrorCode.Value.ToTransactionExecutionError() : null
            });
        }

        [HttpPut]
        public IActionResult RebuildTransactions([Required, FromBody] RebuildTransactionRequest request)
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
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
