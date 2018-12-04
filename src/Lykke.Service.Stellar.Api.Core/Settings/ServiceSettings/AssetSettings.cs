using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Stellar.Api.Core.Settings.ServiceSettings
{
    public class AssetSettings
    {
        public string Id { get; set; }
        public string Address { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public int Accuracy { get; set; }
    }
}
