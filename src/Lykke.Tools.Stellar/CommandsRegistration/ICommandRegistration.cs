using Microsoft.Extensions.CommandLineUtils;

namespace Lykke.Tools.Stellar.CommandsRegistration
{
    public interface ICommandRegistration
    {
        void StartExecution(CommandLineApplication lineApplication);
    }
}
