using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Domain.Balance;

namespace Lykke.Service.Stellar.Api.Core.Services
{
    public interface IBlockchainAssetsService
    {
        Asset GetNativeAsset();
    }
}
