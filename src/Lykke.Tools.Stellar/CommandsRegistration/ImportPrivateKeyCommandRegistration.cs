using System;
using System.Numerics;
using Lykke.Tools.Stellar.Commands;
using Microsoft.Extensions.CommandLineUtils;

namespace Lykke.Tools.Stellar.CommandsRegistration
{
    [CommandRegistration("import-private-key")]
    public class ImportPrivateKeyCommandRegistration : ICommandRegistration
    {
        private readonly CommandFactory _factory;

        public ImportPrivateKeyCommandRegistration(CommandFactory factory)
        {
            _factory = factory;
        }

        public void StartExecution(CommandLineApplication lineApplication)
        {
            lineApplication.Description = "This is the description for scan-deposits-withdraw.";
            lineApplication.HelpOption("-?|-h|--help");

            var signFacadeUrl = lineApplication.Option("-sfu|--sign-facade-url <optionvalue>",
                "Sign Facade Url",
                CommandOptionType.SingleValue);

            var apiKey = lineApplication.Option("-ak|--api-key <optionvalue>",
                "ApiKey With Import Access",
                CommandOptionType.SingleValue);

            var blockchainType = lineApplication.Option("-bt|--blockchain-type <optionvalue>",
                "BlockchainType(Stellar, Kin)",
                CommandOptionType.SingleValue);

            lineApplication.OnExecute(async () =>
            {
                if (!signFacadeUrl.HasValue())
                {
                    Console.WriteLine($"{signFacadeUrl.Description} is empty");
                    return -1;
                }

                if (!apiKey.HasValue())
                {
                    Console.WriteLine($"{apiKey.Description} is empty");
                    return -1;
                }

                if (!blockchainType.HasValue())
                {
                    Console.WriteLine($"{blockchainType.Description} is empty");
                    return -1;
                }

                var command = _factory.CreateCommand((helper) => new ImportPrivateKeyCommand(
                    signFacadeUrl.Value(),
                    apiKey.Value(),
                    blockchainType.Value()));

                return await command.ExecuteAsync();
            });
        }
    }
}

