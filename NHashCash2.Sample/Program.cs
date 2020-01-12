using System;

namespace NHashCash2.Sample
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			string resource = args[0];
			int denomination = int.Parse(args[1]);
			StampFormat format = (StampFormat)Enum.Parse(typeof(StampFormat), $"Version{args[2]}");

			Minter minter = new Minter();
			string stamp = minter.Mint(resource, denomination, DateTimeOffset.UtcNow, format);
			Console.WriteLine(stamp);

			Console.WriteLine("Press a key to exit...");
			Console.ReadKey();
		}
	}
}
