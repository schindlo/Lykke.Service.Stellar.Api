using System.Net;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.BlockchainApi.Contract.Addresses;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Http;

namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("api/addresses")]
    public class AddressesController : Controller
    {
        private readonly IBalanceService _balanceService;

        public AddressesController(IBalanceService balanceService)
        {
            _balanceService = balanceService;
        }

        /// <summary>
        /// Check wallet address validity
        /// </summary>
        [HttpGet("{address}/validity")]
        [ProducesResponseType(typeof(AddressValidationResponse), (int)HttpStatusCode.OK)]
        public IActionResult Validity([Required] string address)
        {
            bool hasExtension;
            return Ok(new AddressValidationResponse
            {
                IsValid = _balanceService.IsAddressValid(address, out hasExtension)
            });
        }

        [HttpGet("{address}/explorer-url")]
        [ProducesResponseType(typeof(List<string>), (int)HttpStatusCode.OK)]
        public IActionResult GetExplorerUrl([Required] string address)
        {
            return new StatusCodeResult(StatusCodes.Status501NotImplemented);
        }
    }
}
