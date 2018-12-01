using Autofac;
using Common.Log;

namespace Lykke.Tools.Erc20Exporter.Helpers
{
    public interface IConfigurationHelper
    {
        (IContainer resolver, ILog logToConsole) GetResolver(string serviceUrl);
    }
}
