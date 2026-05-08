using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Infrastructure.ExternalService.Clarifai;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static Google.Apis.Requests.BatchRequest;

namespace ElecWasteCollection.Infrastructure.ExternalService.Imagga
{
    public class ImaggaImageService : IImageRecognitionService
    {
		private readonly HttpClient _httpClient;
		private readonly ILogger<ImaggaImageService> _logger;
		private readonly ImaggaSettings _settings;
		private readonly double Confidence_AcceptToSave = 30.0;
		private readonly ISystemConfigService _systemConfigService;
		private readonly ClarifaiSettings _clarifaiSettings;
		public ImaggaImageService(ILogger<ImaggaImageService> logger, IOptions<ImaggaSettings> options, ISystemConfigService systemConfigService, IOptions<ClarifaiSettings> optionsc)
		{
			_logger = logger;
			_settings = options.Value;
			_httpClient = new HttpClient();
			_systemConfigService = systemConfigService;
			_clarifaiSettings = optionsc.Value;
		}
		public async Task<ImaggaCheckResult> AnalyzeImageCategoryAsync(string imageUrl, string? aiTags)
		{
			_logger.LogInformation("--- CLARIFAI AI (NATURAL RANKING MODE) START ---");

			// 1. Lấy danh sách tag từ Database (aiTags)
			var acceptedEnglishTags = string.IsNullOrWhiteSpace(aiTags)
				? new List<string> { "electronics", "appliance" }
				: aiTags.Split(',').Select(tag => tag.Trim().ToLower()).ToList();

			// 2. Lấy ngưỡng tự động duyệt từ System Config
			var thresholdConfig = await _systemConfigService.GetSystemConfigByKey("AI_AUTO_APPROVE_THRESHOLD");
			if (!double.TryParse(thresholdConfig?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double minConfidenceThreshold))
			{
				minConfidenceThreshold = 30.0;
			}

			try
			{
				// 3. Cấu hình Request gọi Clarifai
				var pat = _clarifaiSettings.PersonalAccessToken.Trim();
				var requestUrl = "https://api.clarifai.com/v2/users/clarifai/apps/main/models/general-image-recognition/versions/aa7f35c01e0642fda5cf400f543e7c40/outputs";

				using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
				request.Headers.Add("Authorization", $"Key {pat}");

				var jsonBody = new { inputs = new[] { new { data = new { image = new { url = imageUrl } } } } };
				request.Content = new StringContent(JsonSerializer.Serialize(jsonBody), Encoding.UTF8, "application/json");

				// 4. Gửi Request và nhận phản hồi
				var response = await _httpClient.SendAsync(request);
				var responseString = await response.Content.ReadAsStringAsync();

				if (!response.IsSuccessStatusCode)
				{
					_logger.LogError($"[CLARIFAI ERROR] Status: {response.StatusCode}, Details: {responseString}");
					return new ImaggaCheckResult { IsMatch = false };
				}

				// 5. Parse JSON kết quả
				using var doc = JsonDocument.Parse(responseString);
				var concepts = doc.RootElement.GetProperty("outputs")[0].GetProperty("data").GetProperty("concepts").EnumerateArray();

				bool overallMatch = false;
				var allResults = new List<LabelModel>();

				foreach (var concept in concepts)
				{
					string name = concept.GetProperty("name").GetString()?.ToLower() ?? "";
					double conf = Math.Round(concept.GetProperty("value").GetDouble() * 100, 2);

					bool isExactMatch = acceptedEnglishTags.Contains(name);

					// Kiểm tra xem ảnh có đạt chuẩn rác điện tử để tự động duyệt không
					if (!overallMatch && isExactMatch && conf >= minConfidenceThreshold)
						overallMatch = true;

					if (conf > Confidence_AcceptToSave)
					{
						allResults.Add(new LabelModel
						{
							Tag = name,
							Confidence = conf,
							Status = isExactMatch ? "Phù hợp với danh mục" : "Không phù hợp với danh mục"
						});
					}
				}

				// --- 6. LOGIC LỌC VÀ SẮP XẾP THEO YÊU CẦU CỦA TRÍ ---
				var rawTagsLog = string.Join(" | ", allResults.Select(x => $"{x.Tag}: {x.Confidence}%"));
				_logger.LogInformation($"[CLARIFAI RAW RESULTS] TẤT CẢ TAG NHẬN DIỆN ĐƯỢC (> {Confidence_AcceptToSave}%):");
				_logger.LogInformation(rawTagsLog);
				// Nhóm A: Các tag CÓ trong Database (Sắp xếp % cao đến thấp)
				var priorityTags = allResults
					.Where(x => acceptedEnglishTags.Contains(x.Tag))
					.OrderByDescending(x => x.Confidence)
					.ToList();

				// Nhóm B: Các tag KHÔNG CÓ trong Database
				var otherTags = allResults
					.Where(x => !acceptedEnglishTags.Contains(x.Tag))
					.OrderByDescending(x => x.Confidence)
					.ToList();

				var finalLabelsToShow = new List<LabelModel>();
				double currentThreshold = 101.0; // Mốc chặn ban đầu

				// Bước 1: Nạp tất cả tag ưu tiên vào danh sách trước
				foreach (var tag in priorityTags)
				{
					finalLabelsToShow.Add(tag);
					currentThreshold = tag.Confidence; // Mốc chặn bây giờ là điểm của tag ưu tiên cuối cùng
				}

				// Bước 2: Lấp đầy các vị trí còn lại (tối đa 5 tag) 
				// Chỉ lấy tag có % nhỏ hơn tag đứng trước nó
				foreach (var tag in otherTags)
				{
					if (finalLabelsToShow.Count >= 5)
						break;

					if (tag.Confidence < currentThreshold)
					{
						finalLabelsToShow.Add(tag);
						currentThreshold = tag.Confidence; // Hạ mốc chặn xuống theo tag vừa thêm
					}
				}

				_logger.LogInformation($"[CLARIFAI SUCCESS] Final Match: {overallMatch}");

				return new ImaggaCheckResult
				{
					IsMatch = overallMatch,
					DetectedTagsJson = JsonSerializer.Serialize(finalLabelsToShow)
				};
			}
			catch (Exception ex)
			{
				_logger.LogError($"[FATAL ERROR]: {ex.Message}");
				return new ImaggaCheckResult { IsMatch = false };
			}
		}
		//public async Task<ImaggaCheckResult> AnalyzeImageCategoryAsync(string imageUrl, string? aiTags)
		//{
		//	// Xử lý chuỗi tags từ database, nếu rỗng thì dùng mặc định
		//	List<string> acceptedEnglishTags = new List<string> { "electronics", "appliance" };
		//	if (!string.IsNullOrWhiteSpace(aiTags))
		//	{
		//		acceptedEnglishTags = aiTags.Split(',')
		//									.Select(tag => tag.Trim().ToLower())
		//									.ToList();
		//	}

		//	var basicAuthValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ApiKey}:{_settings.ApiSecret}"));
		//	var requestUrl = $"https://api.imagga.com/v2/tags?image_url={Uri.EscapeDataString(imageUrl)}";

		//	using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
		//	request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuthValue);
		//	try
		//	{
		//		var response = await _httpClient.SendAsync(request);
		//		if (!response.IsSuccessStatusCode)
		//		{
		//			var statusCode = response.StatusCode;
		//			var errorContent = await response.Content.ReadAsStringAsync();
		//			Console.WriteLine($"[IMAGGA API FAILED] Status: {statusCode}");
		//			Console.WriteLine($"[IMAGGA API FAILED] Response: {errorContent}");
		//			return new ImaggaCheckResult { IsMatch = false, DetectedTagsJson = null };
		//		}

		//		var jsonResponse = await response.Content.ReadAsStringAsync();
		//		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		//		var imaggaData = JsonSerializer.Deserialize<ImaggaResponse>(jsonResponse, options);
		//		var tags = imaggaData?.Result?.Tags;

		//		var allProcessedLabels = new List<LabelModel>();
		//		bool overallImageMatch = false;

		//		if (tags != null)
		//		{
		//			foreach (var tag in tags)
		//			{
		//				if (!tag.Tag.TryGetValue("en", out var tagName)) continue;

		//				tagName = tagName.ToLower();
		//				double confidence = Math.Round(tag.Confidence, 2);

		//				bool isTagMatch = acceptedEnglishTags.Contains(tagName);
		//				var ConfidenceThreshold = await _systemConfigService.GetSystemConfigByKey(SystemConfigKey.AI_AUTO_APPROVE_THRESHOLD.ToString());

		//				if (!overallImageMatch && isTagMatch && confidence >= double.Parse(ConfidenceThreshold.Value))
		//				{
		//					overallImageMatch = true;
		//				}

		//				// Chỉ lưu các tag có confidence > 30% (để loại bỏ nhiễu)
		//				if (confidence > Confidence_AcceptToSave)
		//				{
		//					allProcessedLabels.Add(new LabelModel
		//					{
		//						Tag = tagName,
		//						Confidence = confidence,
		//						Status = isTagMatch ? "Phù hợp với danh mục" : "Không phù hợp với danh mục"
		//					});
		//				}
		//			}
		//		}

		//		// Sắp xếp danh sách:
		//		// 1. Ưu tiên 1: Lấy các tag "Phù hợp" lên đầu
		//		// 2. Ưu tiên 2: Sắp xếp các tag đó theo confidence giảm dần
		//		var finalLabelsToShow = allProcessedLabels
		//			.OrderByDescending(l => l.Status == "Phù hợp với danh mục") // <-- Ưu tiên 1
		//			.ThenByDescending(l => l.Confidence)           // <-- Ưu tiên 2
		//			.Take(5) // <-- Lấy 5 tag hàng đầu (sẽ bao gồm tag "Phù hợp" trước)
		//			.ToList();

		//		return new ImaggaCheckResult
		//		{
		//			IsMatch = overallImageMatch, // Status của toàn bộ ảnh
		//			DetectedTagsJson = JsonSerializer.Serialize(finalLabelsToShow)
		//		};
		//	}
		//	catch (Exception ex)
		//	{
		//		Console.WriteLine($"[FATAL ERROR] Error processing image {imageUrl}: {ex.Message}");
		//		return new ImaggaCheckResult { IsMatch = false, DetectedTagsJson = null };
		//	}
		//}
	}
}
