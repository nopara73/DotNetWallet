using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
			args = new string[] { "generate-wallet", "a", "w"};

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
				if(args.Length > 1)
				{
					if (!IsCorrectWalletFileName(args[1]))
					{
						WriteLine("Second argument is not correct wallet file name.");
						WriteLine("Correct wallet file name ends with .json");
						WriteLine($"If this argument is not specified the default wallet will be used: {DefaultWalletFileName}");
						Exit();
					}
					walletFileName = args[1];
				}
			}
			#endregion
			#region ShowBalancesCommand
			if (command == "show-balances")
			{
				AssertArgumentsLenght(args.Length, 1, 2);
				if (args.Length > 1)
				{
					if (IsCorrectWalletFileName(args[1]))
						AssertWalletFormat(args[1]);
				}
				walletFileName = args[1];
			}
			#endregion
			#region ShowHistoryCommand
			if (command == "show-history")
			{
				AssertArgumentsLenght(args.Length, 1, 2);
			}
			#endregion
			#region ReceiveCommand
			if (command == "receive")
			{
				AssertArgumentsLenght(args.Length, 1, 2);
			}
			#endregion
			#region SendCommand
			if (command == "send")
			{

			}
			#endregion

			Exit();
		}
		public static void AssertWalletFormat(string walletFileName)
		{
		}
		public static bool IsCorrectWalletFileName(string walletFileName)
		{
			if (walletFileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
				return true;
			return false;
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
