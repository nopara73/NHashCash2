using System;
using Xunit;

namespace NHashCash2.Tests
{
	public class MinterTests
	{
		[Fact]
		public void CheckForFailureOnZeroLengthResource()
		{
			Assert.Throws<ArgumentException>(() =>
			{
				Minter minter = new Minter();
				string stamp = minter.Mint(string.Empty, 8, DateTime.Now, StampFormat.Version0);
			});
		}

		[Fact]
		public void CheckForFailureOnUrlResource()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() =>
			{
				Minter minter = new Minter();
				string stamp = minter.Mint("http://notgartner.com", 8, DateTime.Now, StampFormat.Version0);
			});
		}

		[Fact]
		public void CheckForFailureOnZeroLengthDenomination()
		{
			Assert.Throws<ArgumentOutOfRangeException>(() =>
			{
				Minter minter = new Minter();
				string stamp = minter.Mint("foo0123456789", 0, DateTime.Now, StampFormat.Version0);
			});
		}

		[Fact]
		public void CheckMint16BitVersion0Stamp()
		{
			Minter minter = new Minter();
			minter.Mint("foo0123456789", 16, DateTime.Now, StampFormat.Version0);
		}

		[Fact]
		public void CheckMint16BitVersion1Stamp()
		{
			Minter minter = new Minter();
			minter.Mint("foo0123456789", 16, DateTime.Now, StampFormat.Version1);
		}

		[Fact]
		public void CheckMint20BitVersion0Stamp()
		{
			Minter minter = new Minter();
			minter.Mint("foo0123456789", 20, DateTime.Now, StampFormat.Version0);
		}

		[Fact]
		public void CheckMint20BitVersion1Stamp()
		{
			Minter minter = new Minter();
			minter.Mint("foo0123456789", 20, DateTime.Now, StampFormat.Version1);
		}
	}
}
