using System;
using StellarBase = Stellar;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Lykke.Service.Stellar.Api.Core.Domain.Transaction;
using Lykke.Service.Stellar.Api.Core.Exceptions;
using Lykke.Service.Stellar.Api.Core.Services;
using Newtonsoft.Json.Linq;

namespace Lykke.Service.Stellar.Api.Services
{
    public class StellarService: IStellarService
    {
        private string _horizonUrl = "https://horizon-testnet.stellar.org/";

        private readonly ITxBroadcastRepository _broadcastRepository;

        public StellarService(ITxBroadcastRepository broadcastRepository)
        {
            _broadcastRepository = broadcastRepository;
        }

        public Boolean IsAddressValid(string address)
        {
            try
            {
                StellarBase.StrKey.DecodeCheck(StellarBase.VersionByte.ed25519Publickey, address);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<TxBroadcast> GetTxBroadcastAsync(Guid operationId)
        {
            return await _broadcastRepository.GetAsync(operationId);
        }

        public async Task BroadcastTxAsync(Guid operationId, string xdrBase64)
        {
            try
            {
                var hash = await PostTransactionAsync(xdrBase64);
                if (string.IsNullOrWhiteSpace(hash))
                {
                    throw new ServiceException($"Transaction hash is empty");
                }

                var broadcast = new TxBroadcast
                {
                    OperationId = operationId,
                    State = TxBroadcastState.Completed,
                    Hash = hash
                };
                await _broadcastRepository.AddAsync(broadcast);
            }
            catch (Exception ex)
            {
                var broadcast = new TxBroadcast
                {
                    OperationId = operationId,
                    State = TxBroadcastState.Failed
                };
                await _broadcastRepository.AddAsync(broadcast);

                throw new ServiceException($"Broadcasting transaction failed (operationId: {operationId}).", ex);
            }
        }

        public async Task DeleteTxBroadcastAsync(Guid operationId)
        {
            await _broadcastRepository.DeleteAsync(operationId);
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
