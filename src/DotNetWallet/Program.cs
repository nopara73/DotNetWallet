using DotNetWallet.Helpers;
using DotNetWallet.KeyManagement;
using NBitcoin;
using Newtonsoft.Json.Linq;
using QBitNinja.Client;
using QBitNinja.Client.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using static DotNetWallet.KeyManagement.Safe;
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
			//args = new string[] { "generate-wallet", "wallet-file=test.json" };
			////math super cool donate beach mobile sunny web board kingdom bacon crisp
			////no password
			//args = new string[] { "recover-wallet", "wallet-file=Wallet3.json" };
			//args = new string[] { "show-balances", "wallet-file=test.json" };
			//args = new string[] { "receive", "wallet-file=test.json" };
			//args = new string[] { "show-history", "wallet-file=test.json" };
			//args = new string[] { "send", "btc=0.001", "address=mq6fK8fkFyCy9p53m4Gf4fiX2XCHvcwgi1", "wallet-file=test.json" };
			//args = new string[] { "send", "btc=all", "address=mzKvnpSsrjBXmNngo3t5w7abR5tTWE7Z9V", "wallet-file=test.json" };

			// Load config file
			// It also creates it with default settings if doesn't exist
			Config.Load();

			if (args.Length == 0)
			{
				DisplayHelp();
				Exit(color: ConsoleColor.Green);
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
					Exit($"Wrong argument format specified: {arg}");
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
					
					WriteLine();					
					WriteLine("---------------------------------------------------------------------------");
					WriteLine("Address\t\t\t\t\tConfirmed\tUnconfirmed");
					WriteLine("---------------------------------------------------------------------------");
					var confirmedWallBalance = Money.Zero;
					var unconfirmedWallBalance = Money.Zero;
					foreach (var elem in operationsPerAddresses)
					{
						List<BalanceOperation> operations = elem.Value;

						var confirmedAddrBalance = Money.Zero;
						var unconfirmedAddrBalance = Money.Zero;
						
						foreach (var op in operations)
						{
							if (op.Confirmations > 0)
							{
								confirmedAddrBalance += op.Amount;
							}
							else
							{
								unconfirmedAddrBalance += op.Amount;
							}
						}
						confirmedWallBalance += confirmedAddrBalance;
						unconfirmedWallBalance += unconfirmedAddrBalance;

						if(confirmedAddrBalance != Money.Zero || unconfirmedAddrBalance != Money.Zero)
							WriteLine($"{elem.Key.ToWif()}\t{confirmedAddrBalance}\t{unconfirmedAddrBalance}");
					}
					WriteLine();
					WriteLine("---------------------------------------------------------------------------");
					WriteLine($"Confirmed Wallet Balance: {confirmedWallBalance}");
					WriteLine($"Unconfirmed Wallet Balance: {unconfirmedWallBalance}");
					WriteLine("---------------------------------------------------------------------------");
					WriteLine();

				}
				else if(Config.ConnectionType == ConnectionType.FullNode)
				{
					throw new NotImplementedException();
				}
				else
				{
					Exit("Invalid connection type.");
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
					Dictionary<BitcoinAddress, List<BalanceOperation>> operationsPerAddresses = QueryOperationsPerSafeAddresses(safe);

					WriteLine();
					WriteLine("---------------------------------------------------------------------------");
					WriteLine("Date\t\t\tAmount\t\tConfirmed\tTransaction Id");
					WriteLine("---------------------------------------------------------------------------");

					HashSet<BalanceOperation> opSet = new HashSet<BalanceOperation>();
					foreach (var elem in operationsPerAddresses)
						foreach (var op in elem.Value)
							opSet.Add(op);

					if (opSet.Count() == 0)
					{
						Exit("Wallet has no history yet.");
					}

					var opList = opSet.ToList()
						.OrderByDescending(x => x.Confirmations)
						.ThenBy(x => x.FirstSeen);
					foreach (var op in opList)
					{
						if (op.Amount > 0)
							ForegroundColor = ConsoleColor.Green;
						else if (op.Amount < 0)
							ForegroundColor = ConsoleColor.Red;

						WriteLine($"{op.FirstSeen.DateTime}\t{op.Amount}\t{op.Confirmations > 0}\t\t{op.TransactionId}");

						ResetColor();
					}
					WriteLine();
				}
				else if (Config.ConnectionType == ConnectionType.FullNode)
				{
					throw new NotImplementedException();
				}
				else
				{
					Exit("Invalid connection type.");
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
					Dictionary<BitcoinAddress, List<BalanceOperation>> operationsPerNormalAddresses = QueryOperationsPerSafeAddresses(safe, MinUnusedKeysToQuery, HdPathType.Normal);

					WriteLine();
					WriteLine("---------------------------------------------------------------------------");
					WriteLine("Unused Receive Addresses");
					WriteLine("---------------------------------------------------------------------------");
					foreach (var elem in operationsPerNormalAddresses)
					{
						if(elem.Value.Count == 0)
							WriteLine($"{elem.Key.ToWif()}");
					}
					WriteLine();
				}
				else if (Config.ConnectionType == ConnectionType.FullNode)
				{
					throw new NotImplementedException();
				}
				else
				{
					Exit("Invalid connection type.");
				}
			}
			#endregion
			#region SendCommand
			if (command == "send")
			{
				AssertArgumentsLenght(args.Length, 3, 4);
				var walletFilePath = GetWalletFilePath(args);
				BitcoinAddress addressToSend;
				try
				{			
					addressToSend = BitcoinAddress.Create(GetArgumentValue(args, argName: "address", required: true), Config.Network);
				}
				catch(Exception ex)
				{
					Exit(ex.ToString());
					throw ex;
				}
				Safe safe = DecryptWalletByAskingForPassword(walletFilePath);

				if (Config.ConnectionType == ConnectionType.Http)
				{
					Dictionary<BitcoinAddress, List<BalanceOperation>> operationsPerAddresses = QueryOperationsPerSafeAddresses(safe, MinUnusedKeysToQuery);

					// 1. Gather all the not empty private keys
					WriteLine("Finding not empty private keys...");
					var operationsPerNotEmptyPrivateKeys = new Dictionary<BitcoinExtKey, List<BalanceOperation>>();
					foreach (var elem in operationsPerAddresses)
					{
						var balance = Money.Zero;
						foreach (var op in elem.Value) balance += op.Amount;
						if (balance > Money.Zero)
						{
							var secret = safe.GetPrivateKey(elem.Key);
							operationsPerNotEmptyPrivateKeys.Add(secret, elem.Value);
						}
					}

					// 2. Get the script pubkey of the change.
					WriteLine("Select change address...");
					Script changeScriptPubKey = null;
					Dictionary<BitcoinAddress, List<BalanceOperation>> operationsPerChangeAddresses = QueryOperationsPerSafeAddresses(safe, minUnusedKeys: 1, hdPathType: HdPathType.Change);
					foreach (var elem in operationsPerChangeAddresses)
					{
						if (elem.Value.Count == 0)
							changeScriptPubKey = safe.GetPrivateKey(elem.Key, hdPathType: HdPathType.Change).ScriptPubKey;
					}
					if (changeScriptPubKey == null)
						throw new ArgumentNullException();

					// 3. Gather coins can be spend
					WriteLine("Gathering unspent coins...");
					Dictionary<Coin, bool> unspentCoins = GetUnspentCoins(operationsPerNotEmptyPrivateKeys.Keys);

					// 4. Get the fee
					WriteLine("Calculating transaction fee...");
					Money fee;
					try
					{
						var txSizeInBytes = 250;
						using (var client = new HttpClient())
						{

							const string request = @"https://bitcoinfees.21.co/api/v1/fees/recommended";
							var result = client.GetAsync(request, HttpCompletionOption.ResponseContentRead).Result;
							var json = JObject.Parse(result.Content.ReadAsStringAsync().Result);
							var fastestSatoshiPerByteFee = json.Value<decimal>("fastestFee");
							fee = new Money(fastestSatoshiPerByteFee * txSizeInBytes, MoneyUnit.Satoshi);
						}
					}
					catch
					{
						Exit("Couldn't calculate transaction fee, try it again later.");
						throw new Exception("Can't get tx fee");
					}
					WriteLine($"Fee: {fee.ToDecimal(MoneyUnit.BTC).ToString("0.#############################")}btc");

					// 5. How much money we can spend?
					Money availableAmount = Money.Zero;
					Money unconfirmedAvailableAmount = Money.Zero;
					foreach (var elem in unspentCoins)
					{
						// If can spend unconfirmed add all
						if (Config.CanSpendUnconfirmed)
						{
							availableAmount += elem.Key.Amount;
							if(!elem.Value)
								unconfirmedAvailableAmount += elem.Key.Amount;
						}
						// else only add confirmed ones
						else
						{
							if (elem.Value)
							{
								availableAmount += elem.Key.Amount;
							}
						}
					}

					// 6. How much to spend?
					Money amountToSend = null;
					string amountString = GetArgumentValue(args, argName: "btc", required: true);
					if (string.Equals(amountString, "all", StringComparison.OrdinalIgnoreCase))
					{
						amountToSend = availableAmount;
						amountToSend -= fee;
					}
					else
					{
						amountToSend = ParseBtcString(amountString);
					}

					// 7. Do some checks
					if (amountToSend < Money.Zero || availableAmount < amountToSend + fee)
						Exit("Not enough coins.");

					decimal feePc = Math.Round((100 * fee.ToDecimal(MoneyUnit.BTC)) / amountToSend.ToDecimal(MoneyUnit.BTC));
					if (feePc > 1)
					{
						WriteLine();
						WriteLine($"The transaction fee is {feePc.ToString("0.#")}% of your transaction amount.");
						WriteLine($"Sending:\t {amountToSend.ToDecimal(MoneyUnit.BTC).ToString("0.#############################")}btc");
						WriteLine($"Fee:\t\t {fee.ToDecimal(MoneyUnit.BTC).ToString("0.#############################")}btc");
						ConsoleKey response = GetYesNoAnswerFromUser();
						if (response == ConsoleKey.N)
						{
							Exit("User interruption.");
						}
					}

					var confirmedAvailableAmount = availableAmount - unconfirmedAvailableAmount;
					var totalOutAmount = amountToSend + fee;
					if (confirmedAvailableAmount < totalOutAmount)
					{
						var unconfirmedToSend = totalOutAmount - confirmedAvailableAmount;
						WriteLine();
						WriteLine($"In order to complete this transaction you have to spend {unconfirmedToSend.ToDecimal(MoneyUnit.BTC).ToString("0.#############################")} unconfirmed btc.");
						ConsoleKey response = GetYesNoAnswerFromUser();
						if (response == ConsoleKey.N)
						{
							Exit("User interruption.");
						}
					}

					// 8. Select coins
					WriteLine("Selecting coins...");
					var coinsToSpend = new HashSet<Coin>();
					var unspentConfirmedCoins = new List<Coin>();
					var unspentUnconfirmedCoins = new List<Coin>();
					foreach (var elem in unspentCoins)
						if (elem.Value) unspentConfirmedCoins.Add(elem.Key);
						else unspentUnconfirmedCoins.Add(elem.Key);

					bool haveEnough = SelectCoins(ref coinsToSpend, totalOutAmount, unspentConfirmedCoins);
					if (!haveEnough)
						haveEnough = SelectCoins(ref coinsToSpend, totalOutAmount, unspentUnconfirmedCoins);
					if (!haveEnough)
						throw new Exception("Not enough funds.");

					// 9. Get signing keys
					var signingKeys = new HashSet<ISecret>();
					foreach(var coin in coinsToSpend)
					{
						foreach(var elem in operationsPerNotEmptyPrivateKeys)
						{
							if (elem.Key.ScriptPubKey == coin.ScriptPubKey)
								signingKeys.Add(elem.Key);
						}
					}

					// 10. Build the transaction
					WriteLine("Signing transaction...");
					var builder = new TransactionBuilder();
					var tx = builder
						.AddCoins(coinsToSpend)
						.AddKeys(signingKeys.ToArray())
						.Send(addressToSend, amountToSend)
						.SetChange(changeScriptPubKey)
						.SendFees(fee)
						.BuildTransaction(true);

					if (!builder.Verify(tx))
						Exit("Couldn't build the transaction.");
					
					WriteLine($"Transaction Id: {tx.GetHash()}");

					WriteLine("Broadcasting transaction...");
					var qBitClient = new QBitNinjaClient(Config.Network);
					var broadcastResponse = qBitClient.Broadcast(tx).Result;

					if(broadcastResponse.Error != null)
					{
						Exit($"Error code: {broadcastResponse.Error.ErrorCode} Reason: {broadcastResponse.Error.Reason}");
					}
					if (!broadcastResponse.Success)
					{
						Exit("Couldn't broadcast the transaction.");
					}
					// QBit's succes response is buggy so let's check manually, too
					var getTxResp = qBitClient.GetTransaction(tx.GetHash()).Result;
					if(getTxResp == null)
					{
						Thread.Sleep(3000);
						getTxResp = qBitClient.GetTransaction(tx.GetHash()).Result;
					}
					if(getTxResp == null)
						Exit("Couldn't broadcast the transaction.");

					WriteLine();
					WriteLine("Transaction is successfully propagated on the network.");
				}
				else if (Config.ConnectionType == ConnectionType.FullNode)
				{
					throw new NotImplementedException();
				}
				else
				{
					Exit("Invalid connection type.");
				}
			}
			#endregion

			Exit(color: ConsoleColor.Green);
		}
		#region QBitNinjaJutsus
		private static bool SelectCoins(ref HashSet<Coin> coinsToSpend, Money totalOutAmount, List<Coin> unspentCoins)
		{
			var haveEnough = false;
			foreach (var coin in unspentCoins.OrderByDescending(x => x.Amount))
			{
				coinsToSpend.Add(coin);
				// if doesn't reach amount, continue adding next coin
				if (coinsToSpend.Sum(x => x.Amount) < totalOutAmount) continue;
				else
				{
					haveEnough = true;
					break;
				}
			}

			return haveEnough;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="secrets"></param>
		/// <returns>dictionary with coins and if confirmed</returns>
		private static Dictionary<Coin, bool> GetUnspentCoins(IEnumerable<ISecret> secrets)
		{
			var unspentCoins = new Dictionary<Coin, bool>();
			foreach (var secret in secrets)
			{
				var destination = secret.PrivateKey.ScriptPubKey.GetDestinationAddress(Config.Network);

				var client = new QBitNinjaClient(Config.Network);
				var balanceModel = client.GetBalance(destination, unspentOnly: true).Result;
				foreach (var operation in balanceModel.Operations)
				{
					foreach(var elem in operation.ReceivedCoins.Select(coin => coin as Coin))
					{
						unspentCoins.Add(elem, operation.Confirmations > 0);
					}
				}
			}

			return unspentCoins;
		}
		private static Dictionary<BitcoinAddress, List<BalanceOperation>> QueryOperationsPerSafeAddresses(Safe safe, int minUnusedKeys = 7, HdPathType? hdPathType = null)
		{
			if(hdPathType == null)
			{
				Dictionary<BitcoinAddress, List<BalanceOperation>> operationsPerNormalAddresses = QueryOperationsPerSafeAddresses(safe, MinUnusedKeysToQuery, HdPathType.Normal);
				Dictionary<BitcoinAddress, List<BalanceOperation>> operationsPerChangeAddresses = QueryOperationsPerSafeAddresses(safe, MinUnusedKeysToQuery, HdPathType.Change);

				var operationsPerAllAddresses = new Dictionary<BitcoinAddress, List<BalanceOperation>>();
				foreach (var elem in operationsPerNormalAddresses)
					operationsPerAllAddresses.Add(elem.Key, elem.Value);
				foreach (var elem in operationsPerChangeAddresses)
					operationsPerAllAddresses.Add(elem.Key, elem.Value);
				return operationsPerAllAddresses;
			}

			var addresses = safe.GetFirstNAddresses(minUnusedKeys, hdPathType.GetValueOrDefault());
			//var addresses = FakeData.FakeSafe.GetFirstNAddresses(minUnusedKeys);

			var operationsPerAddresses = new Dictionary<BitcoinAddress, List<BalanceOperation>>();
			var unusedKeyCount = 0;
			foreach (var elem in QueryOperationsPerAddresses(addresses))
			{
				operationsPerAddresses.Add(elem.Key, elem.Value);
				if (elem.Value.Count == 0) unusedKeyCount++;
			}
			WriteLine($"{operationsPerAddresses.Count} {hdPathType} keys are processed.");

			var startIndex = minUnusedKeys;
			while(unusedKeyCount < minUnusedKeys)
			{
				addresses = new HashSet<BitcoinAddress>();
				for(int i = startIndex; i < startIndex + minUnusedKeys; i++)
				{
					addresses.Add(safe.GetAddress(i, hdPathType.GetValueOrDefault()));
					//addresses.Add(FakeData.FakeSafe.GetAddress(i));
				}
				foreach (var elem in QueryOperationsPerAddresses(addresses))
				{
					operationsPerAddresses.Add(elem.Key, elem.Value);
					if (elem.Value.Count == 0) unusedKeyCount++;
				}
				WriteLine($"{operationsPerAddresses.Count} {hdPathType} keys are processed.");
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
		#endregion				
		#region Assertions
		public static void AssertWalletNotExists(string walletFilePath)
		{
			if (File.Exists(walletFilePath))
			{
				Exit($"A wallet, named {walletFilePath} already exists.");
			}
		}
		public static void AssertCorrectNetwork(Network network)
		{
			if (network != Config.Network)
			{
				WriteLine($"The wallet you want to load is on the {network} Bitcoin network.");
				WriteLine($"But your config file specifies {Config.Network} Bitcoin network.");
				Exit();
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
			
			Exit("Incorrect mnemonic format.");
		}
		// Inclusive
		public static void AssertArgumentsLenght(int length, int min, int max)
		{
			if(length < min)
			{
				Exit($"Not enough arguments are specified, minimum: {min}");
			}
			if(length > max)
			{
				Exit($"Too many arguments are specified, maximum: {max}");
			}
		}
		#endregion
		#region CommandLineArgumentStuff
		private static string GetArgumentValue(string[] args, string argName, bool required = true)
		{
			string argValue = "";
			foreach (var arg in args)
			{
				if (arg.StartsWith($"{argName}=", StringComparison.OrdinalIgnoreCase))
				{
					argValue = arg.Substring(arg.IndexOf("=") + 1);
					break;
				}
			}
			if (required && argValue == "")
			{
				Exit($@"'{argName}=' is not specified.");
			}
			return argValue;
		}
		private static string GetWalletFilePath(string[] args)
		{
			string walletFileName = GetArgumentValue(args, "wallet-file", required: false);
			if (walletFileName == "") walletFileName = Config.DefaultWalletFileName;

			var walletDirName = "Wallets";
			Directory.CreateDirectory(walletDirName);
			return Path.Combine(walletDirName, walletFileName);
		}
		#endregion
		#region CommandLineInterface
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
		private static ConsoleKey GetYesNoAnswerFromUser()
		{
			ConsoleKey response;
			do
			{
				WriteLine($"Are you sure you want to proceed? (y/n)");
				response = ReadKey(false).Key;   // true is intercept key (dont show), false is show
				if (response != ConsoleKey.Enter)
					WriteLine();
			} while (response != ConsoleKey.Y && response != ConsoleKey.N);
			return response;
		}
		public static void DisplayHelp()
		{
			WriteLine("Possible commands are:");
			foreach (var cmd in Commands) WriteLine($"\t{cmd}");
		}
		public static void Exit(string reason = "", ConsoleColor color = ConsoleColor.Red)
		{
			ForegroundColor = color;
			WriteLine();
			WriteLine(reason);
			WriteLine("Press Enter to exit...");
			ReadLine();
			ResetColor();
			Environment.Exit(0);
		}
		#endregion
		#region Helpers
		private static Money ParseBtcString(string value)
		{
			decimal amount;
			if (!decimal.TryParse(
						value.Replace(',', '.'),
						NumberStyles.Any,
						CultureInfo.InvariantCulture,
						out amount))
			{
				Exit("Wrong btc amount format.");
			}


			return new Money(amount, MoneyUnit.BTC);
		}
		#endregion
	}
}
