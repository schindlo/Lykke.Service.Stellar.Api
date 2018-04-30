using System.Net;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Lykke.Service.Stellar.Api.Core.Services;
using Lykke.Service.BlockchainApi.Contract.Addresses;
using Lykke.Common.Api.Contract.Responses;

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
            return Ok(new AddressValidationResponse
            {
                IsValid = _balanceService.IsAddressValid(address, out bool hasExtension)
            });
        }

        [HttpGet("{address}/explorer-url")]
        [ProducesResponseType(typeof(List<string>), (int)HttpStatusCode.OK)]
        public IActionResult GetExplorerUrl([Required] string address)
        {
            if (!_balanceService.IsAddressValid(address, out bool hasExtension))
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError("address", "Address must be valid"));
            }

            string baseAddress = _balanceService.GetBaseAddress(address);
            var urls = _balanceService.GetExplorerUrls(baseAddress);
            return Ok(urls);
        }
    }
}
