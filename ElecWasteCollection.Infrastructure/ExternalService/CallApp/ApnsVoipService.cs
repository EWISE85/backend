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
		private const string BundleId = "com.ngocthb.ewise";

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
				var certPem = File.ReadAllText(certPath);
				var x509Cert = X509Certificate2.CreateFromPem(certPem);

				handler.ClientCertificates.Add(x509Cert);
			}
			catch (Exception ex)
			{
				throw new Exception($"Lỗi khi load chứng chỉ APNs: {ex.Message}");
			}

			_httpClient = new HttpClient(handler)
			{
				DefaultRequestVersion = new Version(2, 0),
				DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
			};
		}

		public async Task<bool> SendVoipPushAsync(string deviceToken, object payload)
		{
			if (string.IsNullOrEmpty(deviceToken)) return false;

			var url = $"{ApnsUrl}{deviceToken}";
			var jsonPayload = JsonSerializer.Serialize(payload);

			var request = new HttpRequestMessage(HttpMethod.Post, url)
			{
				Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
			};

			request.Headers.Add("apns-topic", BundleId);      
			request.Headers.Add("apns-push-type", "voip");    
			request.Headers.Add("apns-priority", "10");       
			request.Headers.Add("apns-expiration", "0");    

			try
			{
				var response = await _httpClient.SendAsync(request);

				if (response.IsSuccessStatusCode)
				{
					return true;
				}

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
