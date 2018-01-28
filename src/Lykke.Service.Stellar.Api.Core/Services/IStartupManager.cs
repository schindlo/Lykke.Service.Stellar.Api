using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Services
{
    public interface IStartupManager
    {
        Task StartAsync();
    }
}