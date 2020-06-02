using Lykke.Service.Stellar.Api.Core.Domain;

namespace Lykke.Service.Stellar.Api.Core.Services
{
    public interface IBlockchainAssetsService
    {
        Asset GetNativeAsset();
    }
}
