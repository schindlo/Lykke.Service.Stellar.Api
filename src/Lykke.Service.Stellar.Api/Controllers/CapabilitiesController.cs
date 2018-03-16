using System.Net;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using Lykke.Service.BlockchainApi.Contract.Common;
using Lykke.Service.Stellar.Api.Models;

namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("api/capabilities")]
    public class CapabilitiesController : Controller
    {
        /// <summary>
        /// Indicate if optional operations are supported
        /// </summary>
        [HttpGet]
        [SwaggerOperation("capabilities")]
        [ProducesResponseType(typeof(StellarCapabilitiesResponse), (int)HttpStatusCode.OK)]
        public IActionResult Get()
        {
            return Ok(new StellarCapabilitiesResponse
            {
                IsTransactionsRebuildingSupported = false,
                AreManyInputsSupported = false,
                AreManyOutputsSupported = false
            });
        }

    }
}
