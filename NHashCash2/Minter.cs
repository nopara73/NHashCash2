using System;
using System.Collections;
using System.Security.Cryptography;
using System.Threading;
using System.Text;

namespace NHashCash2
{
	public class Minter
	{
		private byte[] _characterSet = Encoding.ASCII.GetBytes(
			"0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+/="
			);

		private Random _numberGenerator = new Random();

		/// <summary>
		///	Makes sure that the stamp format is either 0 or 1, just in case we
		/// start to support say validating another stamp format but not
		/// producing it in the same ship.
		/// </summary>
		/// <param name="format">A StampFormat instance.</param>
		private void ValidateStampFormat(StampFormat format)
		{
			if ((format != StampFormat.Version0) && (format != StampFormat.Version1))
			{
				throw new NotSupportedException(
					"Only version 0 and version 1 stamps are supported."
					);
			}
		}

		/// <summary>
		/// Selects a character randomly from the character set defined in m_CharacterSet.
		/// </summary>
		/// <returns>A byte representing the character.</returns>
		private byte GetRandomCharacter()
		{
			byte randomCharacter = _characterSet[_numberGenerator.Next(0, _characterSet.Length)];
			return randomCharacter;
		}

		/// <summary>
		/// Counts the number of continious zeros starting from the left hand side
		/// of the stamp to determine the denomination.
		/// </summary>
		/// <param name="stampHash">A byte array representing the SHA1 hash of the stamp.</param>
		/// <returns>The denomination of the stamp.</returns>
		private int GetStampHashDenomination(byte[] stampHash)
		{
			BitArray continiousBits = new BitArray(stampHash);

			int denomination = 0;
			for (int bitIndex = 0; bitIndex < continiousBits.Length; bitIndex++)
			{
				bool bit = continiousBits[bitIndex];

				if (bit != false)
				{
					break;
				}

				denomination++;
			}

			return denomination;
		}

		/// <summary>
		/// Produces a byte array containing characters from the character set using
		/// the character map provided for selection.
		/// </summary>
		/// <param name="characterMap">
		/// An integer array indicating the indexes of chracters to select from the
		/// character set.
		/// </param>
		/// <param name="totalCharactersInMap">
		/// A count of the actual characters in the map.
		/// </param>
		/// <returns>
		/// A byte array of characters selected from the character set.
		/// </returns>
		private byte[] TranslateCharacterMapToBytes(int[] characterMap, int totalCharactersInMap)
		{
			byte[] hashCounterCharacters = new byte[totalCharactersInMap];
			for (int characterMapIndex = 0; characterMapIndex < totalCharactersInMap; characterMapIndex++)
			{
				int characterIndex = characterMap[characterMapIndex];
				hashCounterCharacters[characterMapIndex] = _characterSet[characterIndex];
			}

			return hashCounterCharacters;
		}

		/// <summary>
		/// Takes an integer and converts it to its base n representation where n is defined
		/// by the size of the character set.
		/// </summary>
		/// <param name="hashCounter">The integer to convert.</param>
		/// <returns>A byte array of characters representing the hash counter in base n.</returns>
		private byte[] CreateCounterBasedOnCharacterSet(int hashCounter)
		{
			int quotient = hashCounter;
			int position = 0;

			int[] characterMap = new int[16];

			while (quotient != 0)
			{
				int remainder = quotient % _characterSet.Length;
				characterMap[position] = (byte)remainder;

				quotient /= _characterSet.Length;

				position++;
			}

			int totalCharactersInMap = position;
			byte[] hashCounterCharacters = TranslateCharacterMapToBytes(characterMap, totalCharactersInMap);

			return hashCounterCharacters;
		}

		/// <summary>
		/// This is the main part of the algorithm. It takes a blank template stamp and randomly replaces the
		/// characters after the prefix until
		/// </summary>
		/// <param name="blankStamp"></param>
		/// <param name="requiredDenomination"></param>
		/// <param name="format"></param>
		/// <param name="prefixLength"></param>
		/// <returns></returns>
		private byte[] ComputePartialCollisionStamp(byte[] blankStamp, int requiredDenomination, StampFormat format, int prefixLength)
		{
			byte[] collisionStamp = blankStamp;

			int randomRangeLowerLimit = prefixLength;
			int randomRangeUpperLimit = collisionStamp.Length;

			SHA1Managed provider = new SHA1Managed();

			int hashCounter = 0;

			bool collisionFound = false;
			while (collisionFound == false)
			{
				if (format == StampFormat.Version1)
				{
					byte[] hashCounterBytes = CreateCounterBasedOnCharacterSet(hashCounter);
					Array.Copy(
						hashCounterBytes,
						0,
						collisionStamp,
						collisionStamp.Length - hashCounterBytes.Length,
						hashCounterBytes.Length
						);
					randomRangeUpperLimit = collisionStamp.Length - hashCounterBytes.Length - 1;
					collisionStamp[randomRangeUpperLimit] = 58;
				}

				int bytePosition = _numberGenerator.Next(randomRangeLowerLimit, randomRangeUpperLimit);
				byte characterByte = GetRandomCharacter();
				collisionStamp[bytePosition] = characterByte;

				byte[] collisionStampHash = provider.ComputeHash(collisionStamp);
				collisionFound = IsCollisionOfRequiredDenomination(collisionStampHash, requiredDenomination);

				hashCounter++;
			}

			return collisionStamp;
		}

		/// <summary>
		/// Checks that the SHA1 hash collides with leading zeros up to the required denomination.
		/// </summary>
		/// <param name="collisionStampHash">SHA1 hash of the stamp.</param>
		/// <param name="requiredDenomination">The required denomination to get a true back.</param>
		/// <returns>True if the hash has enough leading zeros, otherwise false.</returns>
		private bool IsCollisionOfRequiredDenomination(byte[] collisionStampHash, int requiredDenomination)
		{
			bool collisionFound = false;

			int stampDenomination = GetStampHashDenomination(collisionStampHash);
			if (stampDenomination >= requiredDenomination)
			{
				collisionFound = true;
			}

			return collisionFound;
		}

		/// <summary>
		/// Finds out what the length of the stamp should be given the prefix and the suggested
		/// 64-byte boundary for SHA1.
		/// </summary>
		/// <param name="prefixLength">The length of the stamp without any padding.</param>
		/// <returns>An integer telling the caller how long the stamp should be all told.</returns>
		private int CalculatePaddedLength(int prefixLength)
		{
			int minimumUnpaddedLength = prefixLength + MinimumRandom;
			int sixtyFourByteBoundaryRemainder = minimumUnpaddedLength % 64;

			int paddedLength;
			if (sixtyFourByteBoundaryRemainder != 0)
			{
				paddedLength = minimumUnpaddedLength + (64 - sixtyFourByteBoundaryRemainder);
			}
			else
			{
				paddedLength = minimumUnpaddedLength;
			}

			return paddedLength;
		}

		/// <summary>
		/// Creates a byte array containing the stamp prefix and is padded out to the required length.
		/// </summary>
		/// <param name="resource">The resource that the stamp is being produced for.</param>
		/// <param name="requiredDenomination">The required denomination of the stamp.</param>
		/// <param name="date">The date that the stamp is to be minted for.</param>
		/// <param name="format">The format of the stamp.</param>
		/// <param name="prefixLength">The length of the stamp prefix after all its elements have been pieced together.</param>
		/// <returns>A byte array containing the stamp prefix and the required amount of padding.</returns>
		private byte[] CreateBlankStamp(string resource, int requiredDenomination, DateTimeOffset date, StampFormat format, out int prefixLength)
		{
			byte[] stampPrefixBytes = GenerateStampPrefixBytes(resource, requiredDenomination, date, format);
			prefixLength = stampPrefixBytes.Length;

			int paddedLength = CalculatePaddedLength(prefixLength);

			byte[] blankStamp = new byte[paddedLength];
			Array.Copy(stampPrefixBytes, blankStamp, stampPrefixBytes.Length);

			return blankStamp;
		}

		/// <summary>
		/// Generates the stamp prefix bytes from the inputs.
		/// </summary>
		/// <param name="resource">The resource that the stamp is being produced for.</param>
		/// <param name="requiredDenomination">The required denomination of the stamp.</param>
		/// <param name="date">The date that the stamp is to be minted for.</param>
		/// <param name="format">The format of the stamp to be produced.</param>
		/// <returns>A byte array containing the stamp prefix.</returns>
		private byte[] GenerateStampPrefixBytes(string resource, int requiredDenomination, DateTimeOffset date, StampFormat format)
		{
			string stampPrefix = null;
			string stampDate = date.ToString("yymmdd");

			switch (format)
			{
				case StampFormat.Version0:
					stampPrefix = string.Format(
						"0:{0}:{1}:",
						stampDate,
						resource
						);
					break;

				case StampFormat.Version1:
					stampPrefix = string.Format(
						"1:{0}:{1}:{2}::",
						requiredDenomination,
						stampDate,
						resource
						);
					break;
			}

			byte[] stampPrefixBytes = Encoding.ASCII.GetBytes(stampPrefix);

			return stampPrefixBytes;
		}

		/// <summary>
		/// Validates that the denomination is greater than one and between the minimum and maximum denominations.
		/// </summary>
		/// <param name="requiredDenomination">The required denomination.</param>
		private void ValidateRequiredDenomination(int requiredDenomination)
		{
			if ((requiredDenomination <= 0) || (requiredDenomination > MaximumDenomination) || (requiredDenomination < MinimumDenomination))
			{
				string message = string.Format(
					"The required denomination must be between {0} and {1} inclusive.",
					MinimumDenomination,
					MaximumDenomination
					);
				throw new ArgumentOutOfRangeException(nameof(requiredDenomination), requiredDenomination, message);
			}
		}

		/// <summary>
		/// Validates that the resource is not null or a zero-length/empty string.
		/// </summary>
		/// <param name="resource"></param>
		private void ValidateResource(string resource)
		{
			if (string.IsNullOrEmpty(resource))
			{
				throw new ArgumentException("The resource cannot be null or zero length.", nameof(resource));
			}
		}

		/// <summary>
		/// Mints a stamp given the input parameters.
		/// </summary>
		/// <param name="resource">The resource that the stamp is to be minted for.</param>
		/// <returns>A string representation of the hashcash stamp.</returns>
		public string Mint(string resource)
		{
			return Mint(resource, DefaultDenomination, DateTimeOffset.UtcNow, DefaultFormat);
		}

		/// <summary>
		/// Mints a stamp given the input parameters.
		/// </summary>
		/// <param name="resource">The resource that the stamp is to be minted for.</param>
		/// <param name="requiredDenomination">The required denomination of the stamp.</param>
		/// <returns>A string representation of the hashcash stamp.</returns>
		public string Mint(string resource, int requiredDenomination)
		{
			return Mint(resource, requiredDenomination, DateTimeOffset.UtcNow, DefaultFormat);
		}

		/// <summary>
		/// Mints a stamp given the input parameters.
		/// </summary>
		/// <param name="resource">The resource that the stamp is to be minted for.</param>
		/// <param name="date">The date that the stamp is to be minted for.</param>
		/// <returns>A string representation of the hashcash stamp.</returns>
		public string Mint(string resource, DateTimeOffset date)
		{
			return Mint(resource, DefaultDenomination, date, DefaultFormat);
		}

		/// <summary>
		/// Mints a stamp given the input parameters.
		/// </summary>
		/// <param name="resource">The resource that the stamp is to be minted for.</param>
		/// <param name="format">The format of the stamp to be produced.</param>
		/// <returns>A string representation of the hashcash stamp.</returns>
		public string Mint(string resource, StampFormat format)
		{
			return Mint(resource, DefaultDenomination, DateTimeOffset.UtcNow, format);
		}

		/// <summary>
		/// Mints a stamp given the input parameters.
		/// </summary>
		/// <param name="resource">The resource that the stamp is to be minted for.</param>
		/// <param name="date">The date that the stamp is to be minted for.</param>
		/// <param name="format">The format of the stamp to be produced.</param>
		/// <returns>A string representation of the hashcash stamp.</returns>
		public string Mint(string resource, DateTimeOffset date, StampFormat format)
		{
			return Mint(resource, DefaultDenomination, date, format);
		}

		/// <summary>
		/// Mints a stamp given the input parameters.
		/// </summary>
		/// <param name="resource">The resource that the stamp is to be minted for.</param>
		/// <param name="requiredDenomination">The required denomination of the stamp.</param>
		/// <param name="date">The date that the stamp is to be minted for.</param>
		/// <returns>A string representation of the hashcash stamp.</returns>
		public string Mint(string resource, int requiredDenomination, DateTimeOffset date)
		{
			return Mint(resource, requiredDenomination, date, DefaultFormat);
		}

		/// <summary>
		/// Mints a stamp given the input parameters.
		/// </summary>
		/// <param name="resource">The resource that the stamp is to be minted for.</param>
		/// <param name="requiredDenomination">The required denomination of the stamp.</param>
		/// <param name="format">The format of the stamp to be produced.</param>
		/// <returns>A string representation of the hashcash stamp.</returns>
		public string Mint(string resource, int requiredDenomination, StampFormat format)
		{
			return Mint(resource, requiredDenomination, DateTimeOffset.UtcNow, format);
		}

		/// <summary>
		/// Mints a stamp given the input parameters.
		/// </summary>
		/// <param name="resource">The resource that the stamp is to be minted for.</param>
		/// <param name="requiredDenomination">The required denomination of the stamp.</param>
		/// <param name="date">The date that the stamp is to be minted for.</param>
		/// <param name="format">The format of the stamp to be produced.</param>
		/// <returns>A string representation of the hashcash stamp.</returns>
		public string Mint(string resource, int requiredDenomination, DateTimeOffset date, StampFormat format)
		{
			ValidateResource(resource);
			ValidateRequiredDenomination(requiredDenomination);
			ValidateStampFormat(format);

			byte[] blankStamp = CreateBlankStamp(resource, requiredDenomination, date, format, out int prefixLength);
			byte[] collisionStamp = ComputePartialCollisionStamp(blankStamp, requiredDenomination, format, prefixLength);

			string stamp = Encoding.ASCII.GetString(collisionStamp);

			return stamp;
		}

		public int DefaultDenomination { get; set; } = 20;

		public StampFormat DefaultFormat { get; set; }

		public int MaximumDenomination { get; set; } = 32;

		public int MinimumDenomination { get; set; } = 16;

		public int MinimumRandom { get; set; } = 16;
	}
}
