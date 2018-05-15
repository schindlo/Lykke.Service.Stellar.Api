using System.Net;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.BlockchainApi.Contract.Common;

namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("api/constants")]
    public class ConstantsController : Controller
    {
        [HttpGet]
        [SwaggerOperation("constants")]
        [ProducesResponseType(typeof(ConstantsResponse), (int)HttpStatusCode.OK)]
        public IActionResult Get()
        {
            var contants = new ConstantsResponse
            { 
                PublicAddressExtension = new PublicAddressExtensionConstantsContract
                {
                    Separator = Constants.PublicAddressExtension.Separator,
                    DisplayName = Constants.PublicAddressExtension.DisplayName,
                    BaseDisplayName = string.Empty
                }
            };
            return Ok(contants);
        }
    }
}
