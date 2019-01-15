# Lykke.Tools.Stellar

- Build tool: dotnet build

- Run tool: dotnet Lykke.Tools.Stellar.dll get-balance -s (url to stellarhorizon) -a (stellar address) -l (ledger number -ph (passphrase))

(Example from dev env dotnet Lykke.Tools.Stellar.dll get-balance -ph "Test SDF Network ; September 2015" -s https://horizon-testnet.stellar.org/ -a GDF4MNKB57VPSF2ZAM36YEXH6TFEXQGQT4IJVR3IOMZQIFC2B44Z4HBL -l 968370)

Running example:
E:\LykkeCity\StellarApi\src\Lykke.Tools.Stellar\bin\Debug\netcoreapp2.1>dotnet Lykke.Tools.Stellar.dll get-balance -ph "Test SDF Network ; September 2015" -s https://horizon-testnet.stellar.org/ -a GDF4MNKB57VPSF2ZAM36YEXH6TFEXQGQT4IJVR3IOMZQIFC2B44Z4HBL -l 968370
INFO: 01-15 10:41:24.809 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Started calculating
INFO: 01-15 10:41:26.844 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 100
INFO: 01-15 10:41:28.112 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 200
INFO: 01-15 10:41:29.431 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 300
INFO: 01-15 10:41:30.706 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 400
INFO: 01-15 10:41:32.078 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 500
INFO: 01-15 10:41:33.324 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 600
INFO: 01-15 10:41:34.672 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 700
INFO: 01-15 10:41:35.936 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 800
INFO: 01-15 10:41:38.354 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 900
INFO: 01-15 10:41:39.622 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 1000
INFO: 01-15 10:41:40.921 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 1100
INFO: 01-15 10:41:42.206 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 1200
INFO: 01-15 10:41:43.525 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 1300
INFO: 01-15 10:41:44.817 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 1400
INFO: 01-15 10:41:46.141 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 1500
INFO: 01-15 10:41:47.427 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 1600
INFO: 01-15 10:41:48.681 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 1700
INFO: 01-15 10:41:49.988 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 1800
INFO: 01-15 10:41:51.306 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 1900
INFO: 01-15 10:41:52.607 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 2000
INFO: 01-15 10:41:53.909 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 2100
INFO: 01-15 10:41:55.162 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 2200
INFO: 01-15 10:41:56.476 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 2300
Press any key to exit.
INFO: 01-15 10:41:57.783 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Processed transactions so far: 2384
INFO: 01-15 10:41:57.784 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Balance so far: 196137971867
INFO: 01-15 10:41:57.784 : Lykke.Tools.Stellar.Helpers.ConfigurationHelper : CalculateBalanceAsync
      Completed!


Balance so far: 196137971867 - is a needed balance