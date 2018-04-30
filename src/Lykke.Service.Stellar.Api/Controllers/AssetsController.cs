using System.Net;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Helpers;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Common.Api.Contract.Responses;

namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("api/assets")]
    public class AssetsController : Controller
    {
        [HttpGet]
        [SwaggerOperation("assets")]
        [ProducesResponseType(typeof(PaginationResponse<AssetResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public IActionResult Get([Required, FromQuery] int take, [FromQuery] string continuation)
        {
            if (take < 1)
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError("take", "Must be positive non zero integer"));
            }
            if (!string.IsNullOrEmpty(continuation))
            {
                return BadRequest(ErrorResponse.Create("Invalid parameter").AddModelError("continuation", "Must be valid continuation token"));
            }

            var assets = new [] { Asset.Stellar.ToAssetResponse() };
            return Ok(PaginationResponse.From("", assets));
        }

        [HttpGet("{assetId}")]
        [SwaggerOperation("assets/")]
        [ProducesResponseType(typeof(AssetResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public IActionResult GetAsset([Required] string assetId)
        {
            if (Asset.Stellar.Id != assetId)
            {
                return NoContent();
            }

            return Ok(Asset.Stellar.ToAssetResponse());
        }
    }
}
