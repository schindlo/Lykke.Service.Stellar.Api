using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Lykke.Service.BlockchainApi.Contract.Testing;
using Microsoft.AspNetCore.Http;

namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("api/testing")]
    public class TestingController : Controller
    {
        [HttpPost("transfers")]
        public IActionResult Transfers([Required, FromBody] TestingTransferRequest request)
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }
    }
}
