namespace Lykke.Service.Stellar.Api.Core.Domain
{
    public class Asset
    {
        public Asset(string id, string address, string name, string typeName, int accuracy) 
            => (Id, Address, Name, TypeName, Accuracy) = (id, address, name, typeName, accuracy);

        public string Id { get; }
        public string Address { get; }
        public string Name { get; }
        public string TypeName { get; }
        public int Accuracy { get; }

        public static Asset Stellar { get; } = new Asset("XLM", "", "Stellar Lumen", "native", 7);
    }
}
