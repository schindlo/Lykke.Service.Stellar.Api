using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.Stellar.Api.Core.Services;
namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("/api/transactions/history")]
    public class TransactionsHistoryController : Controller
    {
        private readonly ITransactionObservationService _txObservationService;

        public TransactionsHistoryController(ITransactionObservationService txObservationService)
        {
            _txObservationService = txObservationService;
        }

        [HttpPost("to/{address}/observation")]
        public async Task<IActionResult> AddAddressToIncomingObservationList(string address)
        {
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
            // TODO
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }

        [HttpGet("from/{address}")]
        public async Task<IActionResult> GetOutgoingHistory(string address, [FromQuery] int take, [FromQuery] string afterHash = "")
        {
            // TODO
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }
    }
}
