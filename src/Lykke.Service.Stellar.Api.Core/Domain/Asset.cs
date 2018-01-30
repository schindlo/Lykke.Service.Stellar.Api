using System;

namespace Lykke.Service.Stellar.Api.Core.Domain
{
    public class Asset
    {

        public Asset(string id, string address, string name, int accuracy) => (Id, Address, Name, Accuracy) = (id, address, name, accuracy);

        public string Id { get; }
        public string Address { get; }
        public string Name { get; }
        public int Accuracy { get; }

        public static Asset Stellar { get; } = new Asset("XLM", "", "Stellar Lumen", 7);
    }
}
