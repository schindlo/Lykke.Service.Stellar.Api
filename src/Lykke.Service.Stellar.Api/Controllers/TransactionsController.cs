using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Common.Api.Contract.Responses;
using Microsoft.AspNetCore.Http;

namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("api/transactions")]
    public class TransactionsController: Controller
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
    }
}
