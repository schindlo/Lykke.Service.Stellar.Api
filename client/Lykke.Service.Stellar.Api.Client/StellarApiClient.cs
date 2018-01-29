using System;
using Common.Log;

namespace Lykke.Service.Stellar.Api.Client
{
    public class StellarApiClient : IStellarApiClient, IDisposable
    {
        private readonly ILog _log;

        public StellarApiClient(string serviceUrl, ILog log)
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
