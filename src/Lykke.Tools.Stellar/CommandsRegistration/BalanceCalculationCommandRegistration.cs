using System.Numerics;
using Lykke.Tools.Stellar.Commands;
using Microsoft.Extensions.CommandLineUtils;

namespace Lykke.Tools.Stellar.CommandsRegistration
{
    [CommandRegistration("get-balance")]
    public class BalanceCalculationCommandRegistration : ICommandRegistration
    {
        private readonly CommandFactory _factory;

        public BalanceCalculationCommandRegistration(CommandFactory factory)
        {
            _factory = factory;
        }

        public void StartExecution(CommandLineApplication lineApplication)
        {
            lineApplication.Description = "This is the description for scan-deposits-withdraw.";
            lineApplication.HelpOption("-?|-h|--help");

            var serviceUrlOption = lineApplication.Option("-s|--settings <optionvalue>",
                "Settings url",
                CommandOptionType.SingleValue);

            var addressOption = lineApplication.Option("-a|--address <optionvalue>",
                "Stellar address",
                CommandOptionType.SingleValue);

            var ledgerOption = lineApplication.Option("-l|--ledger <optionvalue>",
                "Ledger limit",
                CommandOptionType.SingleValue);

            lineApplication.OnExecute(async () =>
            {
                var ledger = ledgerOption.Value();
                BigInteger? latestLedger = null;
                if (BigInteger.TryParse(ledger, out var bigInt))
                {
                    latestLedger = bigInt;
                }

                var command = _factory.CreateCommand((helper) => new BalanceCalculationCommand(helper,
                    serviceUrlOption.Value(),
                    addressOption.Value(),
                    latestLedger
                    ));

                return await command.ExecuteAsync();
            });
        }
    }
}

