using System;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Service.Stellar.Api.Core.Settings.ServiceSettings
{
    public class StellarApiSettings
    {
        public DbSettings Db { get; set; }

        public TimeSpan TransactionExpirationTime { get; set; }

        public string NetworkPassphrase { get; set; }

        [HttpCheck("/")]
        public string HorizonUrl { get; set; }

        public string DepositBaseAddress { get; set; }

        public string[] ExplorerUrlFormats { get; set; }
    }
}
