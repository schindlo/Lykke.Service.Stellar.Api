using System;
using Lykke.Service.BlockchainApi.Contract.Assets;
using Lykke.Service.Stellar.Api.Core.Domain;

namespace Lykke.Service.Stellar.Api.Helpers
{
    public static class Extensions
    {
        public static AssetResponse ToAssetResponse(this Asset self)
        {
            return new AssetResponse
            {
                Accuracy = self.Accuracy,
                Address = self.Address,
                AssetId = self.Id,
                Name = self.Name
            };
        }
    }
}
