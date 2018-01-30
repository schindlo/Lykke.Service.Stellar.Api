using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using Lykke.Service.BlockchainApi.Contract.Common;

namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("api/[controller]")]
    public class CapabilitiesController : Controller
    {
        
        /// <summary>
        /// Indicate if optional operations are supported
        /// </summary>
        [HttpGet]
        [SwaggerOperation("capabilities")]
        [ProducesResponseType(typeof(CapabilitiesResponse), (int)HttpStatusCode.OK)]
        public IActionResult Get()
        {
            return Ok(new CapabilitiesResponse
            {
                IsTransactionsRebuildingSupported = false,
                AreManyInputsSupported = false,
                AreManyOutputsSupported = false
            });
        }

    }
}
