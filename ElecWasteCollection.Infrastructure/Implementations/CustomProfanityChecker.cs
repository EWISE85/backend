using ElecWasteCollection.Application.IServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.Implementations
{
	public class CustomProfanityChecker : IProfanityChecker
	{
		private static readonly HashSet<string> ProfanityList;
		private const string WordlistRelativePath = "Resources/profanity-wordlist.txt";

		// Regex để loại bỏ ký tự không phải chữ cái/số, dùng cho việc kiểm tra lách luật

		private static readonly Regex NonWordCharRegex = new Regex(@"[^\p{L}\p{Nd}\s]", RegexOptions.Compiled);

		private static readonly Regex PhoneNumberRegex = new Regex(
		@"(\+84|84|0)(3|5|7|8|9)\d{8}", // Bắt đầu số 0, 3, 5, 7, 8, 9 và 8 số còn lại
		RegexOptions.Compiled | RegexOptions.IgnoreCase);

		static CustomProfanityChecker()
		{
			try
			{
				string baseDirectory = AppContext.BaseDirectory;
				string wordlistPath = Path.Combine(baseDirectory, WordlistRelativePath);

				if (File.Exists(wordlistPath))
				{
					// Đọc và chuẩn hóa (bỏ dấu) tất cả từ trong file
					var loadedWords = File.ReadAllLines(wordlistPath)
										  .Where(l => !string.IsNullOrWhiteSpace(l))
										  .Select(l => RemoveDiacritics(l.Trim().ToLowerInvariant()))
										  .ToHashSet(StringComparer.OrdinalIgnoreCase);

					ProfanityList = loadedWords;
					Debug.WriteLine($"[ProfanityChecker] Đã tải {ProfanityList.Count} từ cấm từ {WordlistRelativePath}.");
				}
				else
				{
					ProfanityList = new HashSet<string>();
					Debug.WriteLine($"[ProfanityChecker] Cảnh báo: KHÔNG tìm thấy file wordlist tại {wordlistPath}. Bộ lọc rỗng.");
				}
			}
			catch (Exception ex)
			{
				ProfanityList = new HashSet<string>();
				Debug.WriteLine($"[ProfanityChecker] Lỗi khi tải wordlist: {ex.Message}");
			}
		}

		// Hàm loại bỏ dấu Tiếng Việt (đã được tối ưu hóa)
		private static string RemoveDiacritics(string text)
		{
			string normalizedString = text.Normalize(NormalizationForm.FormD);
			var stringBuilder = new StringBuilder();

			foreach (char c in normalizedString)
			{
				// Loại bỏ các dấu ghép nối (dấu mũ, dấu huyền, sắc, hỏi, ngã, nặng)
				if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
				{
					stringBuilder.Append(c);
				}
			}
			// Chuyển về chữ thường và loại bỏ khoảng trắng thừa
			return stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant().Trim();
		}

		// =======================================================
		// IProfanityChecker IMPLEMENTATION
		// =======================================================

		public Task<bool> ContainsProfanityAsync(string text)
		{
			if (string.IsNullOrWhiteSpace(text) || ProfanityList.Count == 0)
			{
				return Task.FromResult(false);
			}

			// 1. Chuẩn hóa văn bản đầu vào: Bỏ dấu và loại bỏ ký tự lách luật
			string cleanText = RemoveDiacritics(text);
			cleanText = NonWordCharRegex.Replace(cleanText, " ");

			// 2. Tách văn bản thành các từ và cụm từ (words/tokens)
			string[] words = cleanText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

			// 3. Kiểm tra từng từ/token
			foreach (var word in words)
			{
				// Từ đã được chuẩn hóa, chữ thường và không dấu
				if (ProfanityList.Contains(word))
				{
					return Task.FromResult(true);
				}
			}

			return Task.FromResult(false);
		}

		public Task<string> CensorTextAsync(string text, string placeholder = "****")
		{
			if (string.IsNullOrWhiteSpace(text) || ProfanityList.Count == 0)
			{
				return Task.FromResult(text);
			}

			// Xây dựng chuỗi Regex pattern động từ danh sách từ cấm (sau khi loại bỏ dấu)
			// Lệnh Select bên dưới loại bỏ dấu TỪ DANH SÁCH GỐC (chưa chuẩn hóa)
			var cleanWords = ProfanityList.Select(w => Regex.Escape(w)).ToArray();

			// Pattern tìm kiếm các từ cấm đã loại bỏ dấu trong văn bản gốc
			// Đây là một ví dụ đơn giản, trong thực tế cần phức tạp hơn để xử lý lách luật
			string pattern = @"(?i)\b(" + string.Join("|", cleanWords) + @")\b";

			// Hàm thay thế: Sử dụng NonSpacingMark để bắt dấu tiếng Việt trong văn bản gốc
			string censoredText = Regex.Replace(text, pattern, match =>
			{
				// Để đảm bảo chỉ thay thế chữ cái, không thay thế dấu câu xung quanh
				return placeholder;
			});

			return Task.FromResult(censoredText);
		}

		public Task<bool> ContainsPhoneNumberAsync(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return Task.FromResult(false);
			}

			bool containsPhone = PhoneNumberRegex.IsMatch(text);
			if (containsPhone)
			{
				Debug.WriteLine("[ProfanityChecker] Phát hiện SỐ ĐIỆN THOẠI.");
			}

			return Task.FromResult(containsPhone);
		}
	}
}