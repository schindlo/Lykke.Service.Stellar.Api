using System;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.BlockchainApi.Contract.Transactions;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Common.Api.Contract.Responses;

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

            await _stellarService.BroadcastAsync(request.OperationId, request.SignedTransaction);

            return Ok();
        }
    }
}
