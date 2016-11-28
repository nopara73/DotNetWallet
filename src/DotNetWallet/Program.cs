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
			"show-balances",
			"show-history",
			"receive",
			"send"
		};
		#endregion

		public const string DefaultWalletFileName = "Wallet.json";

		public static void Main(string[] args)
		{
			args = new string[] { "send", "btc=3.2", "address=1F1tAaz5x1HUXrCNLbtMDqcw6o5GNn4xqX" };

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

			#region HelpCommand
			if (command == "help")
			{
				AssertArgumentsLenght(args.Length, 1, 1);
				DisplayHelp();
			}
			#endregion

			var walletFileName = DefaultWalletFileName;

			#region GenerateWalletCommand
			if (command == "generate-wallet")
			{
				AssertArgumentsLenght(args.Length, 1, 2);
				walletFileName = GetWalletFileName(args);

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
				//AssertArgumentsLenght(args.Length, 1, 2);
				walletFileName = GetWalletFileName(args);
				AssertCorrectWalletFormat(walletFileName);

				try
				{
					var amountToSend = new Money(GetAmountToSend(args), MoneyUnit.BTC);

					//todo check for expected network
					var addressToSend = BitcoinAddress.Create(GetAddressToSend(args));
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
			string walletFileName = DefaultWalletFileName;

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
