# DotNetWallet
Bitcoin wallet implementation in .NET Core.  
The wallet is aimed to be a dedicated wallet for a Bitcoin privacy improvement technique, called [TumbleBit](https://github.com/BUSEC/TumbleBit). TumbleBit will be integrated through [NTumbleBit](https://github.com/NTumbleBit/NTumbleBit).  
  
The wallet can communicate with the network through HTTP API and soon as a full node.  
Right now it has a Command Line Interface. Gui will come later.  
  
##How to use it
### Quickstart
The development is at early stages. You have to clone and build it yourself. See the developer documentation/tutorial on CodeProject: [Build a Bitcoin wallet in C#](https://www.codeproject.com/script/Articles/ArticleVersion.aspx?waid=214550&aid=1115639)    
  
The app is cross-platform, you can try it in any OS. You only need for it [dotnet core](https://www.microsoft.com/net/core).

Run the app once. It will generate 

Run the app. The first time it will generate a `Config.json` for you.

```
{
  "DefaultWalletFileName": "Wallet.json",
  "Network": "Main",
  "ConnectionType": "Http"
}
```

`Network` can be "Main" or "TestNet".  
`ConnectionType` can be "Http" or "FullNode". As of writing this the full node functionality is not implemented yet.  
  
You can use the app by providing it the following command line arguments:  
`help`  
`generate-wallet wallet-file=Wallet2.json`  
`recover-wallet wallet-file=Wallet2.json`  
`show-balances wallet-file=Wallet2.json`  
`show-history wallet-file=Wallet2.json`  
`receive wallet-file=Wallet2.json`  
`send wallet-file=Wallet2.json btc=0.3 address=1Jq8iGGT1idaK9z8H7mrYPynjh9LozHnSH`  
Specifying `wallet-file` is optional. If omitted the `DefaultWalletFileName` will be used from the `Config.json`.
