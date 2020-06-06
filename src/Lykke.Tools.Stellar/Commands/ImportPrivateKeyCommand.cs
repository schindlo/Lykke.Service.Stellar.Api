using Lykke.Service.BlockchainSignFacade.Client;
using System;
using System.Threading.Tasks;
using Lykke.Service.BlockchainSignFacade.Contract.Models;
using stellar_dotnet_sdk;

namespace Lykke.Tools.Stellar.Commands
{
    public class ImportPrivateKeyCommand : ICommand
    {
        private readonly BlockchainSignFacadeClient _signFacadeClient;
        private readonly string _blockchainType;

        public ImportPrivateKeyCommand(string signFacadeUrl, 
            string apiKey, 
            string blockchainType)
        {
            _blockchainType = blockchainType;
            var emptyLog = Lykke.Logs.EmptyLogFactory.Instance.CreateLog(this);
            _signFacadeClient = new Lykke.Service.BlockchainSignFacade.Client.BlockchainSignFacadeClient(signFacadeUrl, apiKey, emptyLog);
        }

        public async Task<int> ExecuteAsync()
        {
            Console.WriteLine("Creating Key");
            var stellarPrivateKeyPair = KeyPair.Random();
            Console.WriteLine($"Address(PublicAddress): {stellarPrivateKeyPair.Address}");
            Console.WriteLine("Importing Key");
            await _signFacadeClient.ImportWalletAsync(_blockchainType, new ImportWalletRequest()
            {
                PrivateKey = stellarPrivateKeyPair.SecretSeed,
                PublicAddress = stellarPrivateKeyPair.Address
            });
            Console.WriteLine("Address has been imported");

            return 0;
        }
    }
}
