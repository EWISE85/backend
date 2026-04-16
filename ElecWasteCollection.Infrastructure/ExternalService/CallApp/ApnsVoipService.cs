using ElecWasteCollection.Application.IServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Net;

namespace ElecWasteCollection.Infrastructure.ExternalService.CallApp
{
	public class ApnsVoipService : IApnsService
	{
		private readonly HttpClient _httpClient;
		private const string BundleId = "com.ngocthb.ewise";
		private const string ApnsUrl = "https://api.sandbox.push.apple.com/3/device/";

		public ApnsVoipService()
		{
			var handler = new HttpClientHandler();
			var certPath = Path.Combine(AppContext.BaseDirectory, "Resources", "cert.pem");

			if (!File.Exists(certPath))
			{
				throw new FileNotFoundException($"Không tìm thấy cert tại: {certPath}");
			}

			try
			{
				// Cách load bền bỉ nhất cho .NET trên Linux/Docker
				var certPem = File.ReadAllText(certPath);

				// Nếu file .pem chứa cả Cert và Key, hàm này sẽ tự hốt cả hai
				var x509Cert = X509Certificate2.CreateFromPem(certPem);

				// Thủ thuật để .NET trên Linux nhận diện được Private Key cho Client Auth
				var certWithKey = new X509Certificate2(x509Cert.Export(X509ContentType.Pkcs12));

				handler.ClientCertificates.Add(certWithKey);
			}
			catch (Exception ex)
			{
				throw new Exception($"Lỗi load chứng chỉ APNs: {ex.Message}");
			}

			_httpClient = new HttpClient(handler)
			{
				// APNs BẮT BUỘC HTTP/2
				DefaultRequestVersion = HttpVersion.Version20,
				DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
			};
		}

		public async Task<bool> SendVoipPushAsync(string deviceToken, object payload)
		{
			if (string.IsNullOrEmpty(deviceToken)) return false;

			var url = $"{ApnsUrl}{deviceToken}";
			var jsonPayload = JsonSerializer.Serialize(payload);

			// Tạo request và ép version 2.0 lần nữa cho chắc
			var request = new HttpRequestMessage(HttpMethod.Post, url)
			{
				Version = HttpVersion.Version20,
				VersionPolicy = HttpVersionPolicy.RequestVersionExact,
				Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
			};

			// VOIP Push BẮT BUỘC phải có đuôi .voip ở topic
			request.Headers.Add("apns-topic", $"{BundleId}.voip");
			request.Headers.Add("apns-push-type", "voip");
			request.Headers.Add("apns-priority", "10"); // High priority cho cuộc gọi
			request.Headers.Add("apns-expiration", "0"); // Hết hạn ngay nếu không gửi được

			try
			{
				var response = await _httpClient.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					Console.WriteLine("APNs: Gửi VoIP Push thành công.");
					return true;
				}

				var errorReason = await response.Content.ReadAsStringAsync();
				Console.WriteLine($"Apple từ chối Push: {response.StatusCode} - {errorReason}");
				return false;
			}
			catch (Exception ex)
			{
				// In ra lỗi chi tiết nhất để debug trong Docker
				Console.WriteLine($"Lỗi kết nối APNs: {ex.Message}");
				if (ex.InnerException != null)
				{
					Console.WriteLine($"Chi tiết lỗi (Inner): {ex.InnerException.Message}");
				}
				return false;
			}
		}
	}
}