using NBitcoin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetWallet.KeyManagement
{
    public class Safe
    {
		private Network _network;
		public Network Network => _network;
		private ExtKey _seedPrivateKey;

		public string WalletFilePath { get; }

		protected Safe(string password, string walletFilePath, Network network, string mnemonicString = null)
		{
			_network = network;

			if (mnemonicString != null)
			{
				SetSeed(password, mnemonicString);
			}

			WalletFilePath = walletFilePath;
		}
		public Safe(Safe safe)
		{
			_network = safe._network;
			_seedPrivateKey = safe._seedPrivateKey;
			WalletFilePath = safe.WalletFilePath;
		}
		/// <summary>
		///     Creates a mnemonic, a seed, encrypts it and stores in the specified path.
		/// </summary>
		/// <param name="mnemonic">empty string</param>
		/// <param name="password"></param>
		/// <param name="walletFilePath"></param>
		/// <param name="network"></param>
		/// <returns>Safe</returns>
		public static Safe Create(out string mnemonic, string password, string walletFilePath, Network network)
		{
			var safe = new Safe(password, walletFilePath, network);

			mnemonic = safe.SetSeed(password).ToString();

			safe.Save(password, walletFilePath, network);

			return safe;
		}
		public static Safe Recover(string mnemonic, string password, string walletFilePath, Network network)
		{
			var safe = new Safe(password, walletFilePath, network, mnemonic);
			safe.Save(password, walletFilePath, network);
			return safe;
		}
		private Mnemonic SetSeed(string password, string mnemonicString = null)
		{
			var mnemonic =
				mnemonicString == null
					? new Mnemonic(Wordlist.English, WordCount.Twelve)
					: new Mnemonic(mnemonicString);

			_seedPrivateKey = mnemonic.DeriveExtKey(password);

			return mnemonic;
		}
		private void SetSeed(ExtKey seedExtKey)
		{
			_seedPrivateKey = seedExtKey;
		}
		private void Save(string password, string walletFilePath, Network network)
		{
			if (File.Exists(walletFilePath))
				throw new Exception("WalletFileAlreadyExists");

			var directoryPath = Path.GetDirectoryName(Path.GetFullPath(walletFilePath));
			if (directoryPath != null) Directory.CreateDirectory(directoryPath);

			var privateKey = _seedPrivateKey.PrivateKey;
			var chainCode = _seedPrivateKey.ChainCode;

			var encryptedBitcoinPrivateKeyString = privateKey.GetEncryptedBitcoinSecret(password, _network).ToWif();
			var chainCodeString = Convert.ToBase64String(chainCode);

			var networkString = network.ToString();

			WalletFileSerializer.Serialize(walletFilePath,
				encryptedBitcoinPrivateKeyString,
				chainCodeString,
				networkString);
		}
		public static Safe Load(string password, string walletFilePath)
		{
			if (!File.Exists(walletFilePath))
				throw new Exception("WalletFileDoesNotExists");

			var walletFileRawContent = WalletFileSerializer.Deserialize(walletFilePath);

			var encryptedBitcoinPrivateKeyString = walletFileRawContent.EncryptedSeed;
			var chainCodeString = walletFileRawContent.ChainCode;

			var chainCode = System.Convert.FromBase64String(chainCodeString);

			Network network;
			var networkString = walletFileRawContent.Network;
			if (networkString == Network.Main.ToString())
				network = Network.Main;
			else if (networkString == Network.TestNet.ToString())
				network = Network.TestNet;
			else throw new Exception("NotRecognizedNetworkInWalletFile");

			var safe = new Safe(password, walletFilePath, network);

			var privateKey = Key.Parse(encryptedBitcoinPrivateKeyString, password, safe._network);
			var seedExtKey = new ExtKey(privateKey, chainCode);
			safe.SetSeed(seedExtKey);

			return safe;
		}
	}
}
