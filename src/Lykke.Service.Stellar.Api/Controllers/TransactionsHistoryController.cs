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
        private readonly IStellarService _stellarService;

        private readonly ITransactionObservationService _txObservationService;

        public TransactionsHistoryController(IStellarService stellarService, ITransactionObservationService txObservationService)
        {
            _stellarService = stellarService;
            _txObservationService = txObservationService;
        }

        [HttpPost("to/{address}/observation")]
        public async Task<IActionResult> AddAddressToIncomingObservationList(string address)
        {
            if(!_stellarService.IsAddressValid(address))
            {
                return BadRequest(ErrorResponse.Create("Invalid address").AddModelError("address", "invalid address"));
            }
            var exists = await _txObservationService.IsIncomingTransactionObservedAsync(address);
            if (exists)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }
            await _txObservationService.AddIncomingTransactionObservationAsync(address);
            return Ok();
        }

        [HttpPost("from/{address}/observation")]
        public async Task<IActionResult> AddAddressToOutgoingObservationList(string address)
        {
            if (!_stellarService.IsAddressValid(address))
            {
                return BadRequest(ErrorResponse.Create("Invalid address").AddModelError("address", "invalid address"));
            }
            var exists = await _txObservationService.IsOutgoingTransactionObservedAsync(address);
            if (exists)
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }
            await _txObservationService.AddOutgoingTransactionObservationAsync(address);
            return Ok();
        }

        [HttpDelete("to/{address}/observation")]
        public async Task<IActionResult> DeleteAddressFromIncomingObservationList(string address)
        {
            var exists = await _txObservationService.IsIncomingTransactionObservedAsync(address);
            if (!exists)
            {
                return new StatusCodeResult(StatusCodes.Status204NoContent);
            }
            await _txObservationService.DeleteIncomingTransactionObservationAsync(address);
            return Ok();
        }

        [HttpDelete("from/{address}/observation")]
        public async Task<IActionResult> DeleteAddressFromOutgoingObservationList(string address)
        {
            var exists = await _txObservationService.IsOutgoingTransactionObservedAsync(address);
            if (!exists)
            {
                return new StatusCodeResult(StatusCodes.Status204NoContent);
            }
            await _txObservationService.DeleteOutgoingTransactionObservationAsync(address);
            return Ok();
        }

        [HttpGet("to/{address}")]
        public async Task<IActionResult> GetIncomingHistory(string address, [FromQuery] int take, [FromQuery] string afterHash = "")
        {
            var exists = await _txObservationService.IsIncomingTransactionObservedAsync(address);
            if(!exists)
            {
                return BadRequest(ErrorResponse.Create("Address not observed").AddModelError("address", "not observed"));
            }
            var transactions = await _txObservationService.GetHistory(Core.Domain.Transaction.TxDirectionType.Incoming, address, take, afterHash);
            return Ok(HistoryToModel(transactions));
        }

        [HttpGet("from/{address}")]
        public async Task<IActionResult> GetOutgoingHistory(string address, [FromQuery] int take, [FromQuery] string afterHash = "")
        {
            var exists = await _txObservationService.IsOutgoingTransactionObservedAsync(address);
            if (!exists)
            {
                return BadRequest(ErrorResponse.Create("Address not observed").AddModelError("address", "not observed"));
            }
            var transactions = await _txObservationService.GetHistory(Core.Domain.Transaction.TxDirectionType.Outgoing, address, take, afterHash);
            return Ok(HistoryToModel(transactions));
        }

        private List<StellarHistoricalTransactionContract> HistoryToModel(List<TxHistory> transactions)
        {
            var ret = new List<StellarHistoricalTransactionContract>();
            foreach (var tx in transactions)
            {
                ret.Add(new StellarHistoricalTransactionContract()
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
