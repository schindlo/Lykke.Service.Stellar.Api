using System.Net;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Helpers;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.BlockchainApi.Contract.Common;
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
