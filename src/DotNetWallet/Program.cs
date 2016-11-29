using DotNetWallet.Helpers;
using DotNetWallet.KeyManagement;
using NBitcoin;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

		public static void Main(string[] args)
		{
			//args = new string[] { "generate-wallet", "wallet-file=Wallet6.json" };
			args = new string[] { "recover-wallet", "wallet-file=Wallet5.json" };

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

			var walletFileName = Config.DefaultWalletFileName;

			#region GenerateWalletCommand
			if (command == "generate-wallet")
			{
				AssertArgumentsLenght(args.Length, 1, 2);
				walletFileName = GetWalletFileName(args);
				AssertWalletNotExists(walletFileName);

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
				Safe safe = Safe.Create(out mnemonic, pw, walletFileName, Config.Network);
				// If no exception thrown the wallet is successfully created.
				WriteLine();
				WriteLine("Wallet is successfully created.");
				WriteLine($"Wallet file: {walletFileName}");

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
				walletFileName = GetWalletFileName(args);
				AssertWalletNotExists(walletFileName);

				WriteLine($"Your software is configured using the Bitcoin {Config.Network} network.");
				WriteLine("Provide your mnemonic words, separated by spaces:");
				var mnemonic = ReadLine();
				AssertCorrectMnemonicFormat(mnemonic);

				WriteLine("Provide your password. Please note the wallet cannot check if your password is correct or not. If you provide a wrong password a wallet will be recovered with your provided mnemonic AND password pair:");
				var password = PasswordConsole.ReadPassword();

				Safe safe = Safe.Recover(mnemonic, password, walletFileName, Config.Network);
				// If no exception thrown the wallet is successfully recovered.
				WriteLine();
				WriteLine("Wallet is successfully recovered.");
				WriteLine($"Wallet file: {walletFileName}");
			}
			#endregion
			#region ShowBalancesCommand
			if (command == "show-balances")
			{
				AssertArgumentsLenght(args.Length, 1, 2);
				walletFileName = GetWalletFileName(args);
				AssertCorrectWalletFormat(walletFileName);
			}
			#endregion
			#region ShowHistoryCommand
			if (command == "show-history")
			{
				AssertArgumentsLenght(args.Length, 1, 2);
				walletFileName = GetWalletFileName(args);
				AssertCorrectWalletFormat(walletFileName);
			}
			#endregion
			#region ReceiveCommand
			if (command == "receive")
			{
				AssertArgumentsLenght(args.Length, 1, 2);
				walletFileName = GetWalletFileName(args);
				AssertCorrectWalletFormat(walletFileName);
			}
			#endregion
			#region SendCommand
			if (command == "send")
			{
				AssertArgumentsLenght(args.Length, 3, 4);
				walletFileName = GetWalletFileName(args);
				AssertCorrectWalletFormat(walletFileName);

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
			}
			#endregion

			Exit();
		}
		public static void AssertWalletNotExists(string walletFileName)
		{
			if(File.Exists(walletFileName))
			{
				WriteLine($"A wallet, named {walletFileName} already exists.");
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

			WriteLine("Incorrect mnemonic format.");
			Exit();
		}
		public static void AssertCorrectWalletFormat(string walletFileName)
		{
			//todo
			throw new NotImplementedException();
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
		private static string GetWalletFileName(string[] args)
		{
			string walletFileName = Config.DefaultWalletFileName;

			foreach (var arg in args)
				if (arg.StartsWith("wallet-file=", StringComparison.OrdinalIgnoreCase))
					walletFileName = arg.Substring(arg.IndexOf("=") + 1);
			return walletFileName;
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
