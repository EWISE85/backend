using ElecWasteCollection.Application.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.ExternalService.CallApp
{
	public class ApnsVoipService : IApnsService
	{
		private readonly HttpClient _httpClient;
		private const string BundleId = "com.ngocthb.ewise.voip";

		// Môi trường Sandbox để test (Thay bằng api.push.apple.com khi lên Production)
		private const string ApnsUrl = "https://api.sandbox.push.apple.com/3/device/";

		public ApnsVoipService()
		{
			// 1. Thiết lập Handler để sử dụng chứng chỉ .pem
			var handler = new HttpClientHandler();

			// Đường dẫn tới file cert.pem trong thư mục Resources của project API sau khi build
			var certPath = Path.Combine(AppContext.BaseDirectory, "Resources", "cert.pem");

			if (!File.Exists(certPath))
			{
				throw new FileNotFoundException($"Không tìm thấy file chứng chỉ tại: {certPath}. Hãy đảm bảo bạn đã chép file vào folder Resources và thiết lập 'Copy to Output Directory'.");
			}

			try
			{
				// Đọc file .pem và tạo chứng chỉ (Yêu cầu .NET 6.0 trở lên)
				var certPem = File.ReadAllText(certPath);
				var x509Cert = X509Certificate2.CreateFromPem(certPem);

				handler.ClientCertificates.Add(x509Cert);
			}
			catch (Exception ex)
			{
				throw new Exception($"Lỗi khi load chứng chỉ APNs: {ex.Message}");
			}

			// 2. Cấu hình HttpClient bắt buộc dùng giao thức HTTP/2 cho Apple APNs
			_httpClient = new HttpClient(handler)
			{
				DefaultRequestVersion = new Version(2, 0),
				DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
			};
		}

		public async Task<bool> SendVoipPushAsync(string deviceToken, object payload)
		{
			if (string.IsNullOrEmpty(deviceToken)) return false;

			// Tạo request POST tới Apple
			var url = $"{ApnsUrl}{deviceToken}";
			var jsonPayload = JsonSerializer.Serialize(payload);

			var request = new HttpRequestMessage(HttpMethod.Post, url)
			{
				Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
			};

			// 3. Thêm các Header bắt buộc cho VoIP Push
			request.Headers.Add("apns-topic", BundleId);      // App ID của bạn
			request.Headers.Add("apns-push-type", "voip");    // Loại push là voip (để kích hoạt CallKit)
			request.Headers.Add("apns-priority", "10");       // Ưu tiên cao nhất (đổ chuông ngay lập tức)
			request.Headers.Add("apns-expiration", "0");      // 0 = hết hạn ngay nếu không gửi được (tránh đổ chuông trễ)

			try
			{
				var response = await _httpClient.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					return true;
				}

				// Log lỗi từ Apple nếu gửi thất bại (Ví dụ: Token hết hạn, sai Bundle ID...)
				var errorReason = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Apple APNs rejected push: {response.StatusCode} - {errorReason}");
				return false;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Lỗi kết nối tới Apple APNs: {ex.Message}");
				return false;
			}
		}
	}
}
