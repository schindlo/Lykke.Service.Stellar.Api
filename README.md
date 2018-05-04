# Lykke.Service.Stellar.Api
Blockchain.Api implementation for the [Stellar](https://www.stellar.org/) ledger based on the [Lykke Blockchains integration](https://docs.google.com/document/d/1KVd-2tg-Ze5-b3kFYh1GUdGn9jvoo7HFO3wH_knpd3U) guide. To integrate with the Stellar network the [csharp-stellar-framework](https://github.com/schindlo/csharp-stellar-framework) is used. The two framework components `csharp-stellar-base` and `csharp-stellar-sdk` are referenced as nuget packages.

A deposit wallet (DW) to hot wallet (HW) cash-in funds flow isn't suited for the Stellar ledger. Because each wallet on the Stellar must maintain a [minimum account balance]( https://www.stellar.org/developers/guides/concepts/fees.html#minimum-account-balance).
However, another approach is to go with a master hot wallet (MHW) and assign a [memo]( https://www.stellar.org/developers/guides/concepts/transactions.html#memo) to each customer. Text- and Id-Memos are indexed by the transaction history methods and exposed under the more general purpose name `DestinationTag`. This means the Lykke Wallet must observe the MHW and assign funds to customer's balance based on the destination tag.

The transaction history methods index operations which alter the account balance including [account creations]( https://www.stellar.org/developers/guides/concepts/list-of-operations.html#create-account), [payments]( https://www.stellar.org/developers/guides/concepts/list-of-operations.html#payment) and [account merges]( https://www.stellar.org/developers/guides/concepts/list-of-operations.html#account-merge). They can return multiple items for a transaction hash, if the transaction contains multiple relevant operations.

Find the `Lykke.Service.Stellar.Sign` module [here](https://github.com/schindlo/Lykke.Service.Stellar.Sign).

# Configuration
Available configuration variables are documented below. See [developing](https://github.com/LykkeCity/lykke.dotnettemplates/tree/master/Lykke.Service.LykkeService#developing) for more information on how to work with app and launch settings.
## Lykke.Job.Stellar.Api
```
"StellarApiService": {
    "Db": {
      // Connection string to the Azure storage account where the StellarApiLog table with logs is stored
      "LogsConnString": "",
      // Connection string to the Azure storage account where data tables for observations, transactions and balances are stored
      "DataConnString": ""
    },
    // The network passphrase used when signing transactions. The following passphrases are currently in use:
    // Test: "Test SDF Network ; September 2015"
    // Live: "Public Global Stellar Network ; September 2015"
    "NetworkPassphrase": "",
    // Address of Horizon REST Api endpoint. The following public endpoints are available:
    // Test: https://horizon-testnet.stellar.org/
    // Live: https://horizon.stellar.org/
    "HorizonUrl": "",
    // Array with url formats of blockchain explorers
    // Test: https://stellar.expert/explorer/testnet/account/{0}, http://testnet.stellarchain.io/address/{0}
    // Live: https://stellar.expert/explorer/public/account/{0}, https://stellarchain.io/address/{0}
    "ExplorerUrlFormats": [ "https://stellar.expert/explorer/testnet/account/{0}", "http://testnet.stellarchain.io/address/{0}" ]
  },
  "StellarApiJob": {
    // Period in seconds of the wallet balance update job
    "WalletBalanceJobPeriodSeconds": 60,
    // Period in seconds of the transaction history update job
    "TransactionHistoryJobPeriodSeconds": 120,
    // Period in seconds of the broadcasts in progress update job
    "BroadcastInProgressJobPeriodSeconds": 120,
    // Size of batches processed by the broadcasts in progress update job
    "BroadcastInProgressJobBatchSize": 100
  },
  "SlackNotifications": {
    "AzureQueue": {
      // Connection string to the Azure storage account where slack notifications are queued
      "ConnectionString": "",
      // The name of the queue for the slack notifications
      "QueueName": ""
    }
  }
}
```
## Lykke.Service.Stellar.Api
```
"StellarApiService": {
    "Db": {
      // Connection string to the Azure storage account where the StellarApiLog table with logs is stored
      "LogsConnString": "",
      // Connection string to the Azure storage account where data tables for observations, transactions and balances are stored
      "DataConnString": ""
    },
    // The network passphrase used when signing transactions. The following passphrases are currently in use:
    // Test: "Test SDF Network ; September 2015"
    // Live: "Public Global Stellar Network ; September 2015"
    "NetworkPassphrase": "",
    // Address of Horizon REST Api endpoint. The following public endpoints are available:
    // Test: https://horizon-testnet.stellar.org/
    // Live: https://horizon.stellar.org/
    "HorizonUrl": "",
    // Array with url formats of blockchain explorers
    // Test: https://stellar.expert/explorer/testnet/account/{0}, http://testnet.stellarchain.io/address/{0}
    // Live: https://stellar.expert/explorer/public/account/{0}, https://stellarchain.io/address/{0}
    "ExplorerUrlFormats": [ "https://stellar.expert/explorer/testnet/account/{0}", "http://testnet.stellarchain.io/address/{0}" ]
  },
  "SlackNotifications": {
    "AzureQueue": {
      // Connection string to the Azure storage account where slack notifications are queued
      "ConnectionString": "",
      // The name of the queue for the slack notifications
      "QueueName": ""
    }
  }
}
```

# Tests
End-to-end tests are available as postman collection. Can be run directly in [postman](https://www.getpostman.com/) or on the command line.
* Install newman:
```sh
npm install -g newman
```
* Start Api and Sign services
* Set URLs in the pre-request script of INIT to point to the Api and Sign service:
```javascript
// set global variables
pm.globals.set("URL", "http://localhost:5000");
pm.globals.set("URL_SIGN", "http://localhost:5001");
```
* Start tests:
```sh
newman run LykkeStellarApiTests.postman_collection.json
```
