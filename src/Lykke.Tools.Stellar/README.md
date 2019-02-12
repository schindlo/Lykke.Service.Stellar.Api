# Lykke.Tools.Stellar

- Build tool: dotnet build

# Import private key to sign facade

- Run tool: dotnet Lykke.Tools.Stellar.dll import-private-key -sfu (url to sign facade) -ak (api key with permission to import) -bt (blockchain type)

(Example from dev env dotnet Lykke.Tools.Stellar.dll import-private-key -sfu http://sign-facade.bil.svc.cluster.local -ak (check in settings service) -bt Kin)


# Read balances

- Run tool: dotnet Lykke.Tools.Stellar.dll get-balance -s (url to stellarhorizon) -a (stellar address) -l (ledger number -ph (passphrase))

(Example from dev env dotnet Lykke.Tools.Stellar.dll get-balance -ph "Test SDF Network ; September 2015" -s https://horizon-testnet.stellar.org/ -a GDF4MNKB57VPSF2ZAM36YEXH6TFEXQGQT4IJVR3IOMZQIFC2B44Z4HBL -l 968370)

Running example:
E:\LykkeCity\StellarApi\src\Lykke.Tools.Stellar\bin\Debug\netcoreapp2.1>dotnet Lykke.Tools.Stellar.dll get-balance -ph "Test SDF Network ; September 2015" -s https://horizon-testnet.stellar.org/ -a GDF4MNKB57VPSF2ZAM36YEXH6TFEXQGQT4IJVR3IOMZQIFC2B44Z4HBL -l 968370
INFO: 01-15 10:41:24.809 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Started calculating
INFO: 01-15 10:41:26.844 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 100
INFO: 01-15 10:41:57.784 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Balance so far: 196137971867
INFO: 01-15 10:41:57.784 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Completed!


Balance so far: 196137971867 - is a needed balance