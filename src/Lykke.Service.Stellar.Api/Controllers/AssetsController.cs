using System.Net;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Helpers;
using Lykke.Service.BlockchainApi.Contract;
using Lykke.Service.BlockchainApi.Contract.Assets;


namespace Lykke.Service.Stellar.Api.Controllers
{
    [Route("api/assets")]
    public class AssetsController : Controller
    {
        [HttpGet]
        [SwaggerOperation("assets")]
        public PaginationResponse<AssetResponse> Get([Required, FromQuery] int take, [FromQuery] string continuation)
        {
            var assets = new AssetResponse[] { Asset.Stellar.ToAssetResponse() };

            return PaginationResponse.From("", assets);
        }

        [HttpGet("{assetId}")]
        [SwaggerOperation("assets/")]
        [ProducesResponseType(typeof(AssetResponse), (int)HttpStatusCode.OK)]
        public IActionResult GetAsset([Required] string assetId)
        {
            if (Asset.Stellar.Id != assetId)
            {
                return NotFound();
            }

            return Ok(Asset.Stellar.ToAssetResponse());
        }
    }
}
