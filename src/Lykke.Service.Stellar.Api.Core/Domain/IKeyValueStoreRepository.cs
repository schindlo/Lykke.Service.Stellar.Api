using System.Threading.Tasks;

namespace Lykke.Service.Stellar.Api.Core.Domain
{
    public interface IKeyValueStoreRepository
    {
        Task<string> GetAsync(string key);

        Task SetAsync(string key, string value);
    }
}
