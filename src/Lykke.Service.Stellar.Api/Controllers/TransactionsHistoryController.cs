using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.Stellar.Api.Models;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.Stellar.Api.Core.Domain;

namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("/api/transactions/history")]
    public class TransactionsHistoryController : Controller
    {
        private readonly IBalanceService _balanceService;
        private readonly ITransactionHistoryService _txHistoryService;

        public TransactionsHistoryController(IBalanceService balanceService,
                                             ITransactionHistoryService txHistoryService)
        {
            _balanceService = balanceService;
            _txHistoryService = txHistoryService;
        }

        [HttpPost("to/{address}/observation")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> AddAddressToIncomingObservationList(string address)
        {
            if (!_balanceService.IsAddressValid(address, out bool hasExtension))
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError("address", "Address must be valid"));
            }
            if (hasExtension && !_balanceService.IsDepositBaseAddress(address))
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError("address", "Public address extension allowed for deposit base address only!"));
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
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> AddAddressToOutgoingObservationList(string address)
        {
            if (!_balanceService.IsAddressValid(address, out bool hasExtension))
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError("address", "Address must be valid"));
            }
            if (hasExtension && !_balanceService.IsDepositBaseAddress(address))
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError("address", "Public address extension allowed for deposit base address only!"));
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
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> DeleteAddressFromIncomingObservationList(string address)
        {
            if (!_balanceService.IsAddressValid(address))
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError("address", "Address must be valid"));
            }
            var exists = await _txHistoryService.IsIncomingTransactionObservedAsync(address);
            if (!exists)
            {
                return NoContent();
            }
            await _txHistoryService.DeleteIncomingTransactionObservationAsync(address);
            return Ok();
        }

        [HttpDelete("from/{address}/observation")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> DeleteAddressFromOutgoingObservationList(string address)
        {
            if (!_balanceService.IsAddressValid(address))
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError("address", "Address must be valid"));
            }
            var exists = await _txHistoryService.IsOutgoingTransactionObservedAsync(address);
            if (!exists)
            {
                return NoContent();
            }
            await _txHistoryService.DeleteOutgoingTransactionObservationAsync(address);
            return Ok();
        }

        [HttpGet("to/{address}")]
        [ProducesResponseType(typeof(List<StellarHistoricalTransactionContract>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetIncomingHistory(string address, [FromQuery] int take, [FromQuery] string afterHash = "")
        {
            if (take < 1)
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError("take", "Must be positive non zero integer"));
            }
            if (!_balanceService.IsAddressValid(address))
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError("address", "Address must be valid"));
            }
            var exists = await _txHistoryService.IsIncomingTransactionObservedAsync(address);
            if (!exists)
            {
                return Ok(new List<StellarHistoricalTransactionContract>());
            }
            var transactions = await _txHistoryService.GetHistory(TxDirectionType.Incoming, address, take, afterHash);
            return Ok(HistoryToModel(transactions));
        }

        [HttpGet("from/{address}")]
        [ProducesResponseType(typeof(List<StellarHistoricalTransactionContract>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> GetOutgoingHistory(string address, [FromQuery] int take, [FromQuery] string afterHash = "")
        {
            if (take < 1)
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError("take", "Must be positive non zero integer"));
            }
            if (!_balanceService.IsAddressValid(address))
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError("address", "Address must be valid"));
            }
            var exists = await _txHistoryService.IsOutgoingTransactionObservedAsync(address);
            if (!exists)
            {
                return Ok(new List<StellarHistoricalTransactionContract>());
            }
            var transactions = await _txHistoryService.GetHistory(TxDirectionType.Outgoing, address, take, afterHash);
            return Ok(HistoryToModel(transactions));
        }

        private List<StellarHistoricalTransactionContract> HistoryToModel(List<TxHistory> transactions)
        {
            var ret = new List<StellarHistoricalTransactionContract>();
            foreach (var tx in transactions)
            {
                var contract = new StellarHistoricalTransactionContract
                {
                    Timestamp = tx.CreatedAt,
                    FromAddress = tx.FromAddress,
                    ToAddress = tx.ToAddress,
                    AssetId = tx.AssetId,
                    Amount = tx.Amount.ToString(),
                    Hash = tx.Hash,
                    PaymentType = tx.PaymentType.ToString(),
                    DestinationTag = tx.Memo
                };
                if (!string.IsNullOrEmpty(tx.Memo)) 
                {
                    var extension = $"{Constants.PublicAddressExtension.Separator}{tx.Memo}";
                    contract.ToAddress += extension;
                }
                ret.Add(contract);
            }
            return ret;
        }
    }
}
