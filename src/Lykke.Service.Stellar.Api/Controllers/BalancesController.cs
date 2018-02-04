using System;
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

namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("api/balances")]
    public class BalancesController : Controller
    {
        private readonly IStellarService _stellarService;

        public BalancesController(IStellarService stellarService)
        {
            _stellarService = stellarService;
        }

        [HttpGet]
        [SwaggerOperation("balances")]
        public async Task<PaginationResponse<WalletBalanceContract>> Get([Required, FromQuery] int take, [FromQuery] string continuation)
        {
            var balances = await _stellarService.GetBalancesAsync(take, continuation);

            var results = new List<WalletBalanceContract>();
            foreach(WalletBalance b in balances.Wallets)
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
            var exists = await _stellarService.IsBalanceObservedAsync(address);
            if (exists)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }
            await _stellarService.AddBalanceObservationAsync(address);
            return Ok();
        }

        [HttpDelete("balances/{address}/observation")]
        public async Task<IActionResult> DeleteObservation([Required] string address)
        {
            var exists = await _stellarService.IsBalanceObservedAsync(address);
            if (!exists)
            {
                return new StatusCodeResult(StatusCodes.Status204NoContent);
            }
            await _stellarService.DeleteBalanceObservationAsync(address);
            return Ok();
        }
    }
}
