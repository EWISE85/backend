using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.ExternalService.Imagga
{
    public class ImaggaImageService : IImageRecognitionService
    {
		private readonly HttpClient _httpClient;
		private readonly ILogger<ImaggaImageService> _logger;
		private readonly ImaggaSettings _settings;
		private readonly double Confidence_AcceptToSave = 30.0;
		private readonly ISystemConfigService _systemConfigService;
		public ImaggaImageService(ILogger<ImaggaImageService> logger, IOptions<ImaggaSettings> options, ISystemConfigService systemConfigService)
		{
			_logger = logger;
			_settings = options.Value;
			_httpClient = new HttpClient();
			_systemConfigService = systemConfigService;
		}
		public async Task<ImaggaCheckResult> AnalyzeImageCategoryAsync(string imageUrl, string category)
        {
			List<string> acceptedEnglishTags = CategoryConverter.GetAcceptedEnglishTags(category);
			var basicAuthValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ApiKey}:{_settings.ApiSecret}"));
			var requestUrl = $"https://api.imagga.com/v2/tags?image_url={Uri.EscapeDataString(imageUrl)}";

			using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuthValue);

			try
			{
				var response = await _httpClient.SendAsync(request);
				if (!response.IsSuccessStatusCode)
				{
					var statusCode = response.StatusCode;
					var errorContent = await response.Content.ReadAsStringAsync();

					// Ghi log lỗi này ra Console hoặc Debugger
					Console.WriteLine($"[IMAGGA API FAILED] Status: {statusCode}");
					Console.WriteLine($"[IMAGGA API FAILED] Response: {errorContent}");
					return new ImaggaCheckResult { IsMatch = false, DetectedTagsJson = null };
				}

				var jsonResponse = await response.Content.ReadAsStringAsync();
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var imaggaData = JsonSerializer.Deserialize<ImaggaResponse>(jsonResponse, options);
				var tags = imaggaData?.Result?.Tags;

				var allProcessedLabels = new List<LabelModel>();
				bool overallImageMatch = false;

				if (tags != null)
				{
					foreach (var tag in tags)
					{
						if (!tag.Tag.TryGetValue("en", out var tagName)) continue;

						tagName = tagName.ToLower();
						double confidence = Math.Round(tag.Confidence, 2);

						// 1. Kiểm tra xem tag có "Phù hợp" hay không
						bool isTagMatch = acceptedEnglishTags.Contains(tagName);
						var ConfidenceThreshold =  _systemConfigService.GetSystemConfigByKey(SystemConfigKey.AI_AUTO_APPROVE_THRESHOLD.ToString());
						// 2. Quyết định status của TOÀN BỘ ẢNH (vẫn cần ngưỡng 80%)
						if (!overallImageMatch && isTagMatch && confidence >= double.Parse(ConfidenceThreshold.Value))
						{
							overallImageMatch = true;
						}

						// 3. Chỉ lưu các tag có confidence > 30% (để loại bỏ nhiễu)
						if (confidence > Confidence_AcceptToSave)
						{
							allProcessedLabels.Add(new LabelModel
							{
								Tag = tagName,
								Confidence = confidence,
								// Gán status "Phù hợp" hoặc "Không phù hợp"
								Status = isTagMatch ? "Phù hợp với danh mục" : "Không phù hợp với danh mục"
							});
						}
					}
				}

				// === PHẦN QUAN TRỌNG NHẤT (SẮP XẾP ƯU TIÊN) ===

				// Sắp xếp danh sách:
				// 1. Ưu tiên 1: Lấy các tag "Phù hợp" lên đầu
				// 2. Ưu tiên 2: Sắp xếp các tag đó theo confidence giảm dần
				var finalLabelsToShow = allProcessedLabels
					.OrderByDescending(l => l.Status == "Phù hợp với danh mục") // <-- Ưu tiên 1
					.ThenByDescending(l => l.Confidence)           // <-- Ưu tiên 2
					.Take(5) // <-- Lấy 5 tag hàng đầu (sẽ bao gồm tag "Phù hợp" trước)
					.ToList();
				// ===============================================

				return new Application.Helper.ImaggaCheckResult
				{
					IsMatch = overallImageMatch, // Status của toàn bộ ảnh
					DetectedTagsJson = JsonSerializer.Serialize(finalLabelsToShow) // JSON của 5 tag đã ưu tiên
				};
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[FATAL ERROR] Error processing image {imageUrl}: {ex.Message}");
				return new ImaggaCheckResult { IsMatch = false, DetectedTagsJson = null };
			}
		}
    }
}
