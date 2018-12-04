using System;
using System.Collections.Generic;
using System.Text;
using Lykke.Service.Stellar.Api.Core.Domain;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Service.Stellar.Api.Services.Assets
{
    public class BlockchainAssetsService: IBlockchainAssetsService
    {
        private readonly Asset _nativeAsset;

        public BlockchainAssetsService(string id,
            string address,
            string name,
            string typeName,
            int accuracy)
        {
            _nativeAsset = new Asset(id, address, name, typeName, accuracy);
        }

        public Asset GetNativeAsset()
        {
            return _nativeAsset;
        }
    }
}
