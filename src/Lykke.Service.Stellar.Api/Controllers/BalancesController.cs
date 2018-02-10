using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Swashbuckle.AspNetCore.SwaggerGen;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Balances;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;
using Lykke.Common.Api.Contract.Responses;

namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("api/balances")]
    public class BalancesController : Controller
    {
        private readonly ITransactionService _transactionService;
        private readonly IBalanceService _balanceService;

        public BalancesController(ITransactionService transactionService, IBalanceService balanceService)
        {
            _transactionService = transactionService;
            _balanceService = balanceService;
        }

        [HttpGet]
        [SwaggerOperation("balances")]
        public async Task<PaginationResponse<WalletBalanceContract>> Get([Required, FromQuery] int take, [FromQuery] string continuation)
        {
            var balances = await _balanceService.GetBalancesAsync(take, continuation);

            var results = new List<WalletBalanceContract>();
            foreach (WalletBalance b in balances.Wallets)
            {
                var result = new WalletBalanceContract
                {
                    Address = b.Address,
                    AssetId = b.AssetId,
                    Balance = b.Balance.ToString(),
                    Block = b.Ledger
                };
                results.Add(result);
            }

            return PaginationResponse.From(balances.ContinuationToken, results);
        }

        [HttpPost("{address}/observation")]
        [SwaggerOperation("balances/")]
        public async Task<IActionResult> AddObservation([Required] string address)
        {
            if (!_balanceService.IsAddressValid(address))
            {
                return BadRequest(ErrorResponse.Create("Invalid address").AddModelError("address", "invalid address"));
            }
            var exists = await _balanceService.IsBalanceObservedAsync(address);
            if (exists)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }
            await _balanceService.AddBalanceObservationAsync(address);
            return Ok();
        }

        [HttpDelete("balances/{address}/observation")]
        public async Task<IActionResult> DeleteObservation([Required] string address)
        {
            var exists = await _balanceService.IsBalanceObservedAsync(address);
            if (!exists)
            {
                return new StatusCodeResult(StatusCodes.Status204NoContent);
            }
            await _balanceService.DeleteBalanceObservationAsync(address);
            return Ok();
        }
    }
}
