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
        private readonly IBlockchainAssetsService _blockchainAssetsService;

        public TransactionsController(ITransactionService transactionService,
                                      IBalanceService balanceService,
                                      IBlockchainAssetsService blockchainAssetsService)
        {
            _transactionService = transactionService;
            _balanceService = balanceService;
            _blockchainAssetsService = blockchainAssetsService;
        }

        [HttpPost("single")]
        [ProducesResponseType(typeof(BuildTransactionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> BuildSingle([Required, FromBody] BuildSingleTransactionRequest request)
        {
            if (request == null || request.OperationId.Equals(Guid.Empty))
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError(nameof(request.OperationId), "Must be valid guid"));
            }

            string xdrBase64;
            var build = await _transactionService.GetTxBuildAsync(request.OperationId);
            if (build != null)
            {
                xdrBase64 = build.XdrBase64;
            }
            else
            {
                string memo = null;

                if (!_balanceService.IsAddressValid(request.FromAddress, out var fromAddressHasExtension))
                {
                    return BadRequest(ErrorResponse.Create($"{nameof(request.FromAddress)} is not a valid"));
                }
                if (!_balanceService.IsAddressValid(request.ToAddress, out var toAddressHasExtension))
                {
                    return BadRequest(ErrorResponse.Create($"{nameof(request.ToAddress)} is not a valid"));
                }

                if (fromAddressHasExtension)
                {
                    if (!_balanceService.IsDepositBaseAddress(request.FromAddress))
                    {
                        return BadRequest(ErrorResponse.Create($"{nameof(request.FromAddress)} is not a valid. Public address extension allowed for deposit base address only!"));
                    }

                    if (!_balanceService.IsDepositBaseAddress(request.ToAddress) || toAddressHasExtension)
                    {
                        return BadRequest(ErrorResponse.Create($"{nameof(request.ToAddress)} is not a valid. Only deposit base address allowed as destination, when sending from address with public address extension!"));
                    }

                    memo = _balanceService.GetPublicAddressExtension(request.FromAddress);
                }
                var toBaseAddress = _balanceService.GetBaseAddress(request.ToAddress);
                if (toAddressHasExtension)
                {
                    memo = _balanceService.GetPublicAddressExtension(request.ToAddress);
                }

                if (request.AssetId != _blockchainAssetsService.GetNativeAsset().Id)
                {
                    return BadRequest(ErrorResponse.Create($"{nameof(request.AssetId)} was not found"));
                }

                long amount;
                try
                {
                    amount = long.Parse(request.Amount);
                }
                catch (FormatException)
                {
                    // too small (e.g. 0.1)
                    return BadRequest(StellarErrorResponse.Create($"Amount is too small. min=1, amount={request.Amount}", BlockchainErrorCode.AmountIsTooSmall));
                }

                var fees = new Fees();
                if (!fromAddressHasExtension)
                {
                    fees = await _transactionService.GetFeesAsync();
                }
                var fromAddressBalance = await _balanceService.GetAddressBalanceAsync(request.FromAddress, fees);

                long requiredBalance;
                if (request.IncludeFee)
                {
                    requiredBalance = amount;
                    amount -= fees.BaseFee;
                }
                else
                {
                    requiredBalance = amount + fees.BaseFee;
                }
                var availableBalance = fromAddressBalance.Balance;
                if (requiredBalance > availableBalance)
                {
                    return BadRequest(StellarErrorResponse.Create($"Not enough balance to create transaction. required={requiredBalance}, available={availableBalance}",
                                                                  BlockchainErrorCode.NotEnoughBalance));
                }

                xdrBase64 = await _transactionService.BuildTransactionAsync(request.OperationId, fromAddressBalance, toBaseAddress, memo, amount);
            }

            return Ok(new BuildTransactionResponse
            {
                TransactionContext = xdrBase64
            });
        }

        [HttpPost("single/receive")]
        public IActionResult ReceiveSingle([Required, FromBody] BuildSingleReceiveTransactionRequest request)
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }

        [HttpPost("broadcast")]
        public async Task<IActionResult> Broadcast([Required, FromBody] BroadcastTransactionRequest request)
        {
            if (request == null || request.OperationId.Equals(Guid.Empty))
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError(nameof(request.OperationId), "Must be valid guid"));
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
                // technical / unknown problem
                if (string.IsNullOrWhiteSpace(ex.ErrorCode)) throw;

                var errorResponse = StellarErrorResponse.Create(ex.Message, (BlockchainErrorCode)Enum.Parse(typeof(BlockchainErrorCode), ex.ErrorCode));
                return BadRequest(errorResponse);
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
            if (operationId.Equals(Guid.Empty))
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError(nameof(operationId), "Must be valid guid"));
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
                Timestamp = broadcast.CreatedAt,
                Amount = broadcast.Amount.ToString(),
                Fee = broadcast.Fee.ToString(),
                Hash = broadcast.Hash,
                Block = broadcast.Ledger,
                Error = broadcast.Error,
                ErrorCode = broadcast.ErrorCode?.ToTransactionExecutionError()
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
