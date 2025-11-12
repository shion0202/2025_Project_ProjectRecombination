using System;
using System.Text;
using System.Security.Cryptography;

namespace Kamgam.PowerPivot
{
	public class UtilsHash
	{
		protected static UTF8Encoding _encoding;
		public static UTF8Encoding Utf8Encoding
		{
			get
			{
				if (_encoding == null)
				{
					_encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
				}
				return _encoding;
			}
		}

		protected static MD5CryptoServiceProvider _md5Provider;
		public static MD5CryptoServiceProvider MD5Provider
		{
			get
			{
				if (_md5Provider == null)
				{
					_md5Provider = new MD5CryptoServiceProvider();
				}
				return _md5Provider;
			}
		}

		protected static SHA1CryptoServiceProvider _sha1Provider;
		public static SHA1CryptoServiceProvider SHA1Provider
		{
			get
			{
				if (_sha1Provider == null)
				{
					_sha1Provider = new SHA1CryptoServiceProvider();
				}
				return _sha1Provider;
			}
		}

		protected static SHA256CryptoServiceProvider _sha256Provider;
		public static SHA256CryptoServiceProvider SHA256Provider
		{
			get
			{
				if (_sha256Provider == null)
				{
					_sha256Provider = new SHA256CryptoServiceProvider();
				}
				return _sha256Provider;
			}
		}

		public static string MD5(string textUtf8)
		{
			byte[] bytes = Utf8Encoding.GetBytes(textUtf8);
			byte[] hashBytes = MD5Provider.ComputeHash(bytes);
			return BytesToString(hashBytes).PadLeft(32, '0').ToLowerInvariant();
		}

		public static string SHA1(string textUtf8)
		{
			byte[] bytes = Utf8Encoding.GetBytes(textUtf8);
			byte[] hashBytes = SHA1Provider.ComputeHash(bytes);
			return BytesToString(hashBytes).PadLeft(32, '0').ToLowerInvariant();
		}

		public static string SHA256(string textUtf8)
		{
			byte[] bytes = Utf8Encoding.GetBytes(textUtf8);
			byte[] hashBytes = SHA256Provider.ComputeHash(bytes);
			return BytesToString(hashBytes).PadLeft(64, '0').ToLowerInvariant();
		}

		public static string BytesToString(byte[] bytes)
		{
			// Convert the encrypted bytes back to a string (base 16)
			string str = "";

			for (int i = 0; i < bytes.Length; i++)
			{
				str += Convert.ToString(bytes[i], 16).PadLeft(2, '0');
			}

			return str;
		}
	}
}
