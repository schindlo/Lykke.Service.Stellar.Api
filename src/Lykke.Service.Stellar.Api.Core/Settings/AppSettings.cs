using Lykke.Service.Stellar.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Stellar.Api.Core.Settings.SlackNotifications;

namespace Lykke.Service.Stellar.Api.Core.Settings
{
    public class AppSettings
    {
        public Stellar.ApiSettings Stellar.ApiService { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
    }
}
