using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Stellar.Api.Services
{
    public class StellarService: IStellarService
    {
        private string _horizonUrl = "https://horizon-testnet.stellar.org/";

        public async Task BroadcastAsync(Guid operationId, string xdrBase64)
        {
            try
            {
                var hash = await PostTransactionAsync(xdrBase64);
                if (string.IsNullOrWhiteSpace(hash))
                {
                    throw new ServiceException($"Transaction hash is empty");
                }
            }
            catch(Exception ex)
            {
                // TODO: update presistence: failed
                throw new ServiceException($"Broadcasting transaction failed (operationId: {operationId}).", ex);
            }

            // TODO: update presistence: broadcasted
        }

        private async Task<string> PostTransactionAsync(string signedTx)
        {
            try
            {
                // TODO: use client from stellar-sdk instead
                using (var client = new HttpClient())
                {
                    var body = new List<KeyValuePair<string, string>>();
                    body.Add(new KeyValuePair<string, string>("tx", signedTx));
                    var formUrlEncodedContent = new FormUrlEncodedContent(body);

                    var response = await client.PostAsync(_horizonUrl + "transactions", formUrlEncodedContent);
                    response.EnsureSuccessStatusCode();
      
                    var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                    return json["hash"].ToString();
                }
            }
            catch (Exception ex)
            {
                throw new HorizonApiException($"Horizon api request failed", ex);
            }
        }
    }
}
