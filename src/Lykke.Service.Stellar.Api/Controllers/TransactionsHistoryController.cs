using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.Stellar.Api.Core.Services;
using System.Collections.Generic;
using System;
using Lykke.Service.Stellar.Api.Models;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;

namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("/api/transactions/history")]
    public class TransactionsHistoryController : Controller
    {
        private readonly IBalanceService _balanceService;
        private readonly ITransactionHistoryService _txHistoryService;

        public TransactionsHistoryController(IBalanceService balanceService, ITransactionHistoryService txHistoryService)
        {
            _balanceService = balanceService;
            _txHistoryService = txHistoryService;
        }

        [HttpPost("to/{address}/observation")]
        public async Task<IActionResult> AddAddressToIncomingObservationList(string address)
        {
            if(!_balanceService.IsAddressValid(address))
            {
                return BadRequest(ErrorResponse.Create("Invalid address").AddModelError("address", "invalid address"));
            }
            var exists = await _txHistoryService.IsIncomingTransactionObservedAsync(address);
            if (exists)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }
            await _txHistoryService.AddIncomingTransactionObservationAsync(address);
            return Ok();
        }

        [HttpPost("from/{address}/observation")]
        public async Task<IActionResult> AddAddressToOutgoingObservationList(string address)
        {
            if (!_balanceService.IsAddressValid(address))
            {
                return BadRequest(ErrorResponse.Create("Invalid address").AddModelError("address", "invalid address"));
            }
            var exists = await _txHistoryService.IsOutgoingTransactionObservedAsync(address);
            if (exists)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }
            await _txHistoryService.AddOutgoingTransactionObservationAsync(address);
            return Ok();
        }

        [HttpDelete("to/{address}/observation")]
        public async Task<IActionResult> DeleteAddressFromIncomingObservationList(string address)
        {
            var exists = await _txHistoryService.IsIncomingTransactionObservedAsync(address);
            if (!exists)
            {
                return new StatusCodeResult(StatusCodes.Status204NoContent);
            }
            await _txHistoryService.DeleteIncomingTransactionObservationAsync(address);
            return Ok();
        }

        [HttpDelete("from/{address}/observation")]
        public async Task<IActionResult> DeleteAddressFromOutgoingObservationList(string address)
        {
            var exists = await _txHistoryService.IsOutgoingTransactionObservedAsync(address);
            if (!exists)
            {
                return new StatusCodeResult(StatusCodes.Status204NoContent);
            }
            await _txHistoryService.DeleteOutgoingTransactionObservationAsync(address);
            return Ok();
        }

        [HttpGet("to/{address}")]
        public async Task<IActionResult> GetIncomingHistory(string address, [FromQuery] int take, [FromQuery] string afterHash = "")
        {
            var exists = await _txHistoryService.IsIncomingTransactionObservedAsync(address);
            if(!exists)
            {
                return BadRequest(ErrorResponse.Create("Address not observed").AddModelError("address", "not observed"));
            }
            var transactions = await _txHistoryService.GetHistory(TxDirectionType.Incoming, address, take, afterHash);
            return Ok(HistoryToModel(transactions));
        }

        [HttpGet("from/{address}")]
        public async Task<IActionResult> GetOutgoingHistory(string address, [FromQuery] int take, [FromQuery] string afterHash = "")
        {
            var exists = await _txHistoryService.IsOutgoingTransactionObservedAsync(address);
            if (!exists)
            {
                return BadRequest(ErrorResponse.Create("Address not observed").AddModelError("address", "not observed"));
            }
            var transactions = await _txHistoryService.GetHistory(TxDirectionType.Outgoing, address, take, afterHash);
            return Ok(HistoryToModel(transactions));
        }

        private List<StellarHistoricalTransactionContract> HistoryToModel(List<TxHistory> transactions)
        {
            var ret = new List<StellarHistoricalTransactionContract>();
            foreach (var tx in transactions)
            {
                ret.Add(new StellarHistoricalTransactionContract
                {
                    OperationId = tx.OperationId.HasValue ? tx.OperationId.Value : Guid.Empty,
                    Timestamp = tx.CreatedAt,
                    FromAddress = tx.FromAddress,
                    ToAddress = tx.ToAddress,
                    AssetId = tx.AssetId,
                    Amount = tx.Amount.ToString(),
                    Hash = tx.Hash
                });
            }
            return ret;
        }
    }
}
