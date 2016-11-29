using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetWallet.Helpers
{
	// http://stackoverflow.com/a/7049688/2061103
	/// <summary>
	/// Adds some nice help to the console. Static extension methods don't exist (probably for a good reason) so the next best thing is congruent naming.
	/// </summary>
	public class PasswordConsole
    {
		/// <summary>
		/// Like System.Console.ReadLine(), only with a mask.
		/// </summary>
		/// <param name="mask">a <c>char</c> representing your choice of console mask</param>
		/// <returns>the string the user typed in </returns>
		public static string ReadPassword(char mask)
		{
			const int ENTER = 13, BACKSP = 8, CTRLBACKSP = 127;
			int[] FILTERED = { 0, 27, 9, 10 /*, 32 space, if you care */ }; // const

			var pass = new Stack<char>();
			char chr = (char)0;

			while ((chr = Console.ReadKey(true).KeyChar) != ENTER)
			{
				if (chr == BACKSP)
				{
					if (pass.Count > 0)
					{
						Console.Write("\b \b");
						pass.Pop();
					}
				}
				else if (chr == CTRLBACKSP)
				{
					while (pass.Count > 0)
					{
						Console.Write("\b \b");
						pass.Pop();
					}
				}
				else if (FILTERED.Count(x => chr == x) > 0) { }
				else
				{
					pass.Push((char)chr);
					Console.Write(mask);
				}
			}

			Console.WriteLine();

			return new string(pass.Reverse().ToArray());
		}

		/// <summary>
		/// Like System.Console.ReadLine(), only with a mask.
		/// </summary>
		/// <returns>the string the user typed in </returns>
		public static string ReadPassword()
		{
			return ReadPassword('*');
		}
	}
}
