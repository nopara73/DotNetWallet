using System;
using System.IO;
using Newtonsoft.Json;
using NBitcoin;

namespace DotNetWallet
{
	public static class Config
	{
		// Initialized with default attributes
		public static string DefaultWalletFileName = @"Wallet.json";
		public static Network Network = Network.Main;

		static Config()
		{
			if (!File.Exists(ConfigFileSerializer.ConfigFilePath))
				Save();
			Load();
		}

		public static void Load()
		{
			var rawContent = ConfigFileSerializer.Deserialize();

			DefaultWalletFileName = rawContent.DefaultWalletFileName;

			if (rawContent.Network == "Main")
				Network = Network.Main;
			else if (rawContent.Network == "TestNet")
				Network = Network.TestNet;
			else if(rawContent.Network == null)
				throw new Exception($"Network is missing from {ConfigFileSerializer.ConfigFilePath}");
			else
				throw new Exception($"Wrong Network is specified in {ConfigFileSerializer.ConfigFilePath}");
		}
		public static void Save()
		{
			var networkString = Network.Name;

			ConfigFileSerializer.Serialize(DefaultWalletFileName, networkString);
			Load();
		}
	}
    public class ConfigFileSerializer
	{
		public static string ConfigFilePath = "Config.json";
		// KEEP THEM PUBLIC OTHERWISE IT WILL NOT SERIALIZE!
		public string DefaultWalletFileName { get; set; }
		public string Network { get; set; }

		[JsonConstructor]
		private ConfigFileSerializer(string walletFileName, string network)
		{
			DefaultWalletFileName = walletFileName;
			Network = network;
		}

		internal static void Serialize(string walletFileName, string network)
		{
			var content =
				JsonConvert.SerializeObject(new ConfigFileSerializer(walletFileName, network), Formatting.Indented);

			File.WriteAllText(ConfigFilePath, content);
		}

		internal static ConfigFileSerializer Deserialize()
		{
			if (!File.Exists(ConfigFilePath))
				throw new Exception($"Config file does not exist. Create {ConfigFilePath} before reading it.");

			var contentString = File.ReadAllText(ConfigFilePath);
			var configFileSerializer = JsonConvert.DeserializeObject<ConfigFileSerializer>(contentString);

			return new ConfigFileSerializer(configFileSerializer.DefaultWalletFileName, configFileSerializer.Network);
		}
	}
}
