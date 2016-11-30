using DotNetWallet.Helpers;
using DotNetWallet.KeyManagement;
using NBitcoin;
using QBitNinja.Client;
using QBitNinja.Client.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using static System.Console;

namespace DotNetWallet
{
	public class Program
	{
		#region Commands
		public static HashSet<string> Commands = new HashSet<string>()
		{
			"help",
			"generate-wallet",
			"recover-wallet",
			"show-balances",
			"show-history",
			"receive",
			"send"
		};
		#endregion

		public const int MinUnusedKeysToQuery = 7;

		public static void Main(string[] args)
		{
			//args = new string[] { "help" };
			//args = new string[] { "generate-wallet" };
			//args = new string[] { "generate-wallet", "wallet-file=Wallet4.json" };
			////math super cool donate beach mobile sunny web board kingdom bacon crisp
			////no password
			//args = new string[] { "recover-wallet", "wallet-file=Wallet3.json" };
			//args = new string[] { "show-balances" };
			args = new string[] { "receive" };

			// Load config file
			// It also creates it with default settings if doesn't exist
			Config.Load();

			if (args.Length == 0)
			{
				WriteLine("No command is specified.");
				DisplayHelp();
				Exit();
			}
			var command = args[0];
			if (!Commands.Contains(command))
			{
				WriteLine("Wrong command is specified.");
				DisplayHelp();
			}
			foreach(var arg in args.Skip(1))
			{
				if(!arg.Contains('='))
				{
					WriteLine($"Wrong argument format specified: {arg}");
					Exit();
				}
			}

			#region HelpCommand
			if (command == "help")
			{
				AssertArgumentsLenght(args.Length, 1, 1);
				DisplayHelp();
			}
			#endregion			
			#region GenerateWalletCommand
			if (command == "generate-wallet")
			{
				AssertArgumentsLenght(args.Length, 1, 2);
				var walletFilePath = GetWalletFilePath(args);
				AssertWalletNotExists(walletFilePath);

				string pw;
				string pwConf;
				do
				{
					// 1. Get password from user
					WriteLine("Choose a password:");
					pw = PasswordConsole.ReadPassword();
					// 2. Get password confirmation from user
					WriteLine("Confirm password:");
					pwConf = PasswordConsole.ReadPassword();

					if (pw != pwConf) WriteLine("Passwords do not match. Try again!");
				} while (pw != pwConf);

				// 3. Create wallet
				string mnemonic;
				Safe safe = Safe.Create(out mnemonic, pw, walletFilePath, Config.Network);
				// If no exception thrown the wallet is successfully created.
				WriteLine();
				WriteLine("Wallet is successfully created.");
				WriteLine($"Wallet file: {walletFilePath}");

				// 4. Display mnemonic
				WriteLine();
				WriteLine("Write down the following mnemonic words.");
				WriteLine("With the mnemonic words AND your password you can recover this wallet by using the recover-wallet command.");
				WriteLine();
				WriteLine("-------");
				WriteLine(mnemonic);
				WriteLine("-------");
				WriteLine();
			}
			#endregion
			#region RecoverWalletCommand
			if (command == "recover-wallet")
			{
				AssertArgumentsLenght(args.Length, 1, 2);
				var walletFilePath = GetWalletFilePath(args);
				AssertWalletNotExists(walletFilePath);

				WriteLine($"Your software is configured using the Bitcoin {Config.Network} network.");
				WriteLine("Provide your mnemonic words, separated by spaces:");
				var mnemonic = ReadLine();
				AssertCorrectMnemonicFormat(mnemonic);

				WriteLine("Provide your password. Please note the wallet cannot check if your password is correct or not. If you provide a wrong password a wallet will be recovered with your provided mnemonic AND password pair:");
				var password = PasswordConsole.ReadPassword();

				Safe safe = Safe.Recover(mnemonic, password, walletFilePath, Config.Network);
				// If no exception thrown the wallet is successfully recovered.
				WriteLine();
				WriteLine("Wallet is successfully recovered.");
				WriteLine($"Wallet file: {walletFilePath}");
			}
			#endregion
			#region ShowBalancesCommand
			if (command == "show-balances")
			{
				AssertArgumentsLenght(args.Length, 1, 2);
				var walletFilePath = GetWalletFilePath(args);
				Safe safe = DecryptWalletByAskingForPassword(walletFilePath);

				if(Config.ConnectionType == ConnectionType.Http)
				{
					Dictionary<BitcoinAddress, List<BalanceOperation>> operationsPerAddresses = QueryOperationsPerSafeAddresses(safe, MinUnusedKeysToQuery);

					foreach(var elem in operationsPerAddresses)
					{
					}

					WriteLine();
					WriteLine("---------------------------------------------------------------------------");
					WriteLine($"Confirmed Wallet Balance: {1}");
					WriteLine($"Unconfirmed Wallet Balance: {1}");
					WriteLine("---------------------------------------------------------------------------");
					WriteLine("Address\t\t\t\t\tConfirmed\tUnconfirmed");
					WriteLine("---------------------------------------------------------------------------");
					foreach (var elem in operationsPerAddresses)
					{
						WriteLine($"{elem.Key.ToWif()}");
					}

				}
				else if(Config.ConnectionType == ConnectionType.FullNode)
				{
					//todo
					throw new NotImplementedException();
				}
				else
				{
					WriteLine("Invalid connection type.");
					Exit();
				}
			}
			#endregion
			#region ShowHistoryCommand
			if (command == "show-history")
			{
				AssertArgumentsLenght(args.Length, 1, 2);
				var walletFilePath = GetWalletFilePath(args);
				Safe safe = DecryptWalletByAskingForPassword(walletFilePath);

				if (Config.ConnectionType == ConnectionType.Http)
				{
					Dictionary<BitcoinAddress, List<BalanceOperation>> operationsPerAddresses = QueryOperationsPerSafeAddresses(safe, MinUnusedKeysToQuery);
				}
				else if (Config.ConnectionType == ConnectionType.FullNode)
				{
					//todo
					throw new NotImplementedException();
				}
				else
				{
					WriteLine("Invalid connection type.");
					Exit();
				}
			}
			#endregion
			#region ReceiveCommand
			if (command == "receive")
			{
				AssertArgumentsLenght(args.Length, 1, 2);
				var walletFilePath = GetWalletFilePath(args);
				Safe safe = DecryptWalletByAskingForPassword(walletFilePath);

				if (Config.ConnectionType == ConnectionType.Http)
				{
					Dictionary<BitcoinAddress, List<BalanceOperation>> operationsPerAddresses = QueryOperationsPerSafeAddresses(safe, MinUnusedKeysToQuery);

					WriteLine();
					WriteLine("---------------------------------------------------------------------------");
					WriteLine("Unused Receive Addresses");
					WriteLine("---------------------------------------------------------------------------");
					foreach (var elem in operationsPerAddresses)
					{
						if(elem.Value.Count == 0)
							WriteLine($"{elem.Key.ToWif()}");
					}
					WriteLine();
				}
				else if (Config.ConnectionType == ConnectionType.FullNode)
				{
					//todo
					throw new NotImplementedException();
				}
				else
				{
					WriteLine("Invalid connection type.");
					Exit();
				}
			}
			#endregion
			#region SendCommand
			if (command == "send")
			{
				AssertArgumentsLenght(args.Length, 3, 4);
				var walletFilePath = GetWalletFilePath(args);
				try
				{
					var amountToSend = new Money(GetAmountToSend(args), MoneyUnit.BTC);
					
					var addressToSend = BitcoinAddress.Create(GetAddressToSend(args), Config.Network);
				}
				catch(Exception ex)
				{
					WriteLine(ex);
					Exit();
				}
				Safe safe = DecryptWalletByAskingForPassword(walletFilePath);

				if (Config.ConnectionType == ConnectionType.Http)
				{
					//todo
				}
				else if (Config.ConnectionType == ConnectionType.FullNode)
				{
					//todo
					throw new NotImplementedException();
				}
				else
				{
					WriteLine("Invalid connection type.");
					Exit();
				}
			}
			#endregion

			Exit();
		}

		private static Dictionary<BitcoinAddress, List<BalanceOperation>> QueryOperationsPerSafeAddresses(Safe safe, int minUnusedKeys = 7)
		{
			var addresses = safe.GetFirstNAddresses(minUnusedKeys);

			var operationsPerAddresses = new Dictionary<BitcoinAddress, List<BalanceOperation>>();
			var unusedKeyCount = 0;
			foreach (var elem in QueryOperationsPerAddresses(addresses))
			{
				operationsPerAddresses.Add(elem.Key, elem.Value);
				if (elem.Value.Count == 0) unusedKeyCount++;
			}

			var startIndex = minUnusedKeys;
			while(unusedKeyCount < minUnusedKeys)
			{
				WriteLine($"{startIndex} keys are processed.");
				addresses = new HashSet<BitcoinAddress>();
				for(int i = startIndex; i < startIndex + minUnusedKeys; i++)
				{
					addresses.Add(safe.GetAddress(i));
				}
				foreach (var elem in QueryOperationsPerAddresses(addresses))
				{
					operationsPerAddresses.Add(elem.Key, elem.Value);
					if (elem.Value.Count == 0) unusedKeyCount++;
				}
				startIndex += minUnusedKeys;
			}

			return operationsPerAddresses;
		}
		private static Dictionary<BitcoinAddress, List<BalanceOperation>> QueryOperationsPerAddresses(HashSet<BitcoinAddress> addresses)
		{
			var operationsPerAddresses = new Dictionary<BitcoinAddress, List<BalanceOperation>>();
			var client = new QBitNinjaClient(Config.Network);
			foreach (var addr in addresses)
			{
				var operations = client.GetBalance(addr, unspentOnly: false).Result.Operations;
				operationsPerAddresses.Add(addr, operations);
			}
			return operationsPerAddresses;
		}
		private static Safe DecryptWalletByAskingForPassword(string walletFilePath)
		{
			Safe safe = null;
			string pw;
			bool correctPw = false;
			WriteLine("Type your password:");
			do
			{
				pw = PasswordConsole.ReadPassword();
				try
				{
					safe = Safe.Load(pw, walletFilePath);
					AssertCorrectNetwork(safe.Network);
					correctPw = true;
				}
				catch (System.Security.SecurityException)
				{
					WriteLine("Invalid password, try again:");
					correctPw = false;
				}
			} while (!correctPw);

			if (safe == null)
				throw new Exception("Wallet could not be decrypted.");
			WriteLine($"{walletFilePath} wallet is decrypted.");
			return safe;
		}
		public static void AssertWalletNotExists(string walletFilePath)
		{
			if(File.Exists(walletFilePath))
			{
				WriteLine($"A wallet, named {walletFilePath} already exists.");
				Exit();
			}
		}
		public static void AssertCorrectNetwork(Network network)
		{
			if(network != Config.Network)
			{
				WriteLine($"The wallet you want to load is on the {network} Bitcoin network.");
				WriteLine($"But your config file specifies {Config.Network} Bitcoin network.");
			}
		}
		public static void AssertCorrectMnemonicFormat(string mnemonic)
		{
			try
			{
				if (new Mnemonic(mnemonic).IsValidChecksum)
					return;
			}
			catch (FormatException) { }
			catch (NotSupportedException) { }

			WriteLine("Incorrect mnemonic format.");
			Exit();
		}
		private static string GetAddressToSend(string[] args)
		{
			string address = "";
			foreach (var arg in args)
				if (arg.StartsWith("address=", StringComparison.OrdinalIgnoreCase))
					address = arg.Substring(arg.IndexOf("=") + 1);
			if (address == "")
			{
				WriteLine(@"'address=' is not specified.");
				Exit();
			}
			return address;
		}
		private static decimal GetAmountToSend(string[] args)
		{
			decimal amount = -1m;
			foreach (var arg in args)
				if (arg.StartsWith("btc=", StringComparison.OrdinalIgnoreCase))
					if (!decimal.TryParse(
						arg.Substring(arg.IndexOf("=") + 1).Replace(',', '.'),
						NumberStyles.Any,
						CultureInfo.InvariantCulture,
						out amount))
					{
						WriteLine("Wrong btc amount format.");
						Exit();
					}
			if(amount == -1m)
			{
				WriteLine(@"'btc=' is not specified.");
				Exit();
			}
			return amount;
		}
		private static string GetWalletFilePath(string[] args)
		{
			string walletFileName = Config.DefaultWalletFileName;

			foreach (var arg in args)
				if (arg.StartsWith("wallet-file=", StringComparison.OrdinalIgnoreCase))
					walletFileName = arg.Substring(arg.IndexOf("=") + 1);
			var walDirName = "Wallets";
			Directory.CreateDirectory(walDirName);
			return Path.Combine(walDirName, walletFileName);
		}
		// Inclusive
		public static void AssertArgumentsLenght(int length, int min, int max)
		{
			if(length < min)
			{
				WriteLine($"Not enough arguments are specified, minimum: {min}");
				Exit();
			}
			if(length > max)
			{
				WriteLine($"Too many arguments are specified, maximum: {max}");
				Exit();
			}
		}
		public static void DisplayHelp()
		{
			WriteLine("Possible commands are:");
			foreach (var cmd in Commands) WriteLine($"\t{cmd}");
		}
		public static void Exit()
		{
			WriteLine("Press Enter to exit...");
			ReadLine();
			Environment.Exit(0);
		}
	}
}
