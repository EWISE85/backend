using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Helper
{
	public static class QrMathHelper
	{
		private const long MOD = 10000000000000;
		private const long A = 8191654321987;    
		private const long B = 1020304050607;    

		private static readonly DateTime Epoch = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		private static readonly long A_Inverse = ModInverse(A, MOD);

		/// <summary>
		/// Hàm Hash ổn định: Biến string bất kỳ thành số từ 0 -> 99999
		/// </summary>
		public static int GetStableShortId(string inputId)
		{
			if (string.IsNullOrEmpty(inputId)) return 0;
			unchecked
			{
				uint hash = 2166136261;
				foreach (byte b in Encoding.UTF8.GetBytes(inputId.Trim().ToUpper()))
				{
					hash = (hash ^ b) * 16777619;
				}
				return (int)(hash % 100000);
			}
		}

		public static string Encrypt(int shortId)
		{
			long currentMinutes = (long)(DateTime.UtcNow - Epoch).TotalMinutes;
			long rawData = (long)shortId * 100000000 + currentMinutes;
			BigInteger encrypted = (new BigInteger(rawData) * A + B) % MOD;
			return encrypted.ToString().PadLeft(13, '0');
		}

		public static (int ShortId, bool IsTimeValid) Decrypt(string qrCode)
		{
			if (string.IsNullOrEmpty(qrCode) || qrCode.Length != 13) return (-1, false);
			if (!long.TryParse(qrCode, out long y)) return (-1, false);
			BigInteger bigY = y;
			BigInteger diff = bigY - B;
			while (diff < 0) diff += MOD;

			long raw = (long)((diff * A_Inverse) % MOD);

			int shortId = (int)(raw / 100000000); 
			long timeMinutes = raw % 100000000;  

			long currentMinutes = (long)(DateTime.UtcNow - Epoch).TotalMinutes;
			bool isValid = Math.Abs(currentMinutes - timeMinutes) <= 5;

			return (shortId, isValid);
		}

		private static long ModInverse(long a, long m)
		{
			long m0 = m, y = 0, x = 1;
			if (m == 1) return 0;
			while (a > 1)
			{
				long q = a / m; long t = m; m = a % m; a = t; t = y; y = x - q * y; x = t;
			}
			if (x < 0) x += m0;
			return x;
		}
	}
}
