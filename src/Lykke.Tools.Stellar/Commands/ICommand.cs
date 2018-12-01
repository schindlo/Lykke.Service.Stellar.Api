using System.Threading.Tasks;

namespace Lykke.Tools.Stellar.Commands
{
    public interface ICommand
    {
        Task<int> ExecuteAsync();
    }
}
