using System;
using Common.Log;

namespace Lykke.Service.Stellar.Api.Client
{
    public class Stellar.ApiClient : IStellar.ApiClient, IDisposable
    {
        private readonly ILog _log;

        public Stellar.ApiClient(string serviceUrl, ILog log)
        {
            _log = log;
        }

        public void Dispose()
        {
            //if (_service == null)
            //    return;
            //_service.Dispose();
            //_service = null;
        }
    }
}
