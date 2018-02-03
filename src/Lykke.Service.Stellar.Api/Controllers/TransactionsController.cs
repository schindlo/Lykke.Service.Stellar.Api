using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Stellar.Api.Core.Services;

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
                Hash = broadcast.Hash,
                State = (BroadcastedTransactionState)broadcast.State,
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
