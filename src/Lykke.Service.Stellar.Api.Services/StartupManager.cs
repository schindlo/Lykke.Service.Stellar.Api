using System.Threading.Tasks;
using Lykke.Service.Stellar.Api.Core.Services;

namespace Lykke.Service.Stellar.Api.Services
{
    // NOTE: Sometimes, startup process which is expressed explicitly is not just better, 
    // but the only way. If this is your case, use this class to manage startup.
    // For example, sometimes some state should be restored before any periodical handler will be started, 
    // or any incoming message will be processed and so on.
    // Do not forget to remove As<IStartable>() and AutoActivate() from DI registartions of services, 
    // which you want to startup explicitly.

    public class StartupManager : IStartupManager
    {
        public async Task StartAsync()
        {
            await Task.CompletedTask;
        }
    }
}
