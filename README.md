# DotNetWallet
Bitcoin wallet implementation in .NET Core.  

See the documentation on CodeProject: [Build a Bitcoin wallet in C#](https://www.codeproject.com/script/Articles/ArticleVersion.aspx?waid=214550&aid=1115639)  

##Usage  
First time running the app it will generate the `Config.json` for you:  

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
