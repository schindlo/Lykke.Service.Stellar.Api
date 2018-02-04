using System.Net;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.BlockchainApi.Contract.Addresses;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("api/addresses")]
    public class AddressesController : Controller
    {
        private readonly IStellarService _stellarService;

        public AddressesController(IStellarService stellarService)
        {
            _stellarService = stellarService;
        }

        /// <summary>
        /// Check wallet address validity
        /// </summary>
        [HttpGet("{address}/validity")]
        [SwaggerOperation("addresses/")]
        [ProducesResponseType(typeof(AddressValidationResponse), (int)HttpStatusCode.OK)]
        public IActionResult Validity([Required] string address)
        {
            return Ok(new AddressValidationResponse
            {
                IsValid = _stellarService.IsAddressValid(address)
            });
        }

    }
}
