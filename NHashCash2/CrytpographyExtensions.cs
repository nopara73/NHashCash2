using System;
using System.Collections.Generic;
using System.Text;

namespace System.Security.Cryptography
{
	public static class CrytpographyExtensions
	{
		/// <summary>
		/// https://khalidabuhakmeh.com/creating-random-numbers-with-dotnet-core
		/// WARNING: .NET Core 3 slight bias: https://github.com/dotnet/corefx/pull/31243
		/// </summary>
		public static int Next(this RandomNumberGenerator generator, int min, int max)
		{
			// match Next of Random
			// where max is exclusive
			max -= 1;

			var bytes = new byte[sizeof(int)]; // 4 bytes
			generator.GetNonZeroBytes(bytes);
			var val = BitConverter.ToInt32(bytes, 0);
			// constrain our values to between our min and max
			// https://stackoverflow.com/a/3057867/86411
			var result = ((val - min) % (max - min + 1) + (max - min + 1)) % (max - min + 1) + min;
			return result;
		}
	}
}
