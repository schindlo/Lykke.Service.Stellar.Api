using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("/api/transactions/history")]
    public class TransactionsHistoryController : Controller
    {
        [HttpPost("to/{address}")]
        public async Task<IActionResult> AddAddressToIncomingObservationList(string address)
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }

        [HttpPost("from/{address}")]
        public async Task<IActionResult> AddAddressToOutgoingObservationList(string address)
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }

        [HttpDelete("to/{address}")]
        public async Task<IActionResult> DeleteAddressFromIncomingObservationList(string address)
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }

        [HttpDelete("from/{address}")]
        public async Task<IActionResult> DeleteAddressFromOutgoingObservationList(string address)
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }

        [HttpGet("to/{address}")]
        public async Task<IActionResult> GetIncomingHistory(string address, [FromQuery] int take, [FromQuery] string afterHash = "")
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }

        [HttpGet("from/{address}")]
        public async Task<IActionResult> GetOutgoingHistory(string address, [FromQuery] int take, [FromQuery] string afterHash = "")
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }
    }
}
