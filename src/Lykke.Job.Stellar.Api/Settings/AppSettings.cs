namespace Lykke.Job.Stellar.Api.Settings
{
    public class AppSettings : Service.Stellar.Api.Core.Settings.AppSettings
    {
        public StellarJobSettings StellarApiJob { get; set; }
    }
}
