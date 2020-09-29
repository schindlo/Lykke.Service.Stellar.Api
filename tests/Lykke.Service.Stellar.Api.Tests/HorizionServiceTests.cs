using System.Net.Http;
using System.Threading.Tasks;
using Lykke.Service.Stellar.Api.Core.Settings;
using Lykke.Service.Stellar.Api.Core.Settings.ServiceSettings;
using Lykke.Service.Stellar.Api.Services.Horizon;
using Moq;
using stellar_dotnet_sdk;
using Xunit;

namespace Lykke.Service.Stellar.Api.Tests
{
    public class HorizionServiceTests
    {
        private HorizonService _service;

        public HorizionServiceTests()
        {
            var httpClientFactory = new Mock<IHttpClientFactory>();
            var settings = new AppSettings
            {
                StellarApiService = new StellarApiSettings
                {
                    NetworkPassphrase = "Public", HorizonUrl = "https://horizon-testnet.stellar.org"
                }
            };
            httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
            _service = new HorizonService(settings, httpClientFactory.Object, new Server(settings.StellarApiService.HorizonUrl));
        }

        [Fact]
        public async Task Should_Return_Null_On_UnexistingAddress()
        {
            var result = await _service.GetAccountDetails("GDB7DGHKANDVS3F4N3AZH2AYZL6Q36PZ5EP6LEMGGLQC3N6BI76UHOIZ");
            Assert.Null(result);
        }
    }
}
