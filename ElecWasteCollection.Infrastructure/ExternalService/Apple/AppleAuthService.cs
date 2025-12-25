using ElecWasteCollection.Application.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace ElecWasteCollection.Infrastructure.ExternalService.Apple
{
	public class AppleAuthService : IAppleAuthService
	{
		private readonly IHttpClientFactory _httpClientFactory; 
		private readonly AppleAuthSettings _settings;
		private static JsonWebKeySet _cachedKeys;
		private static DateTime _lastKeyUpdate = DateTime.MinValue;
		private readonly ILogger<AppleAuthService> _logger; 
		public AppleAuthService(IHttpClientFactory httpClientFactory,IOptions<AppleAuthSettings> settings,ILogger<AppleAuthService> logger)
		{
			_httpClientFactory = httpClientFactory;
			_settings = settings.Value;
			_logger = logger;
		}

		public async Task<string?> ValidateTokenAndGetAppleUserIdAsync(string identityToken)
		{
			try
			{
				if (_cachedKeys == null || (DateTime.UtcNow - _lastKeyUpdate).TotalHours > 24)
				{
					_logger.LogInformation("Đang tải Public Keys mới từ Apple...");
					var client = _httpClientFactory.CreateClient(); 
					var json = await client.GetStringAsync(_settings.KeysUrl); 
					_cachedKeys = new JsonWebKeySet(json);
					_lastKeyUpdate = DateTime.UtcNow;
				}

				var parameters = new TokenValidationParameters
				{
					ValidIssuer = "https://appleid.apple.com", 
					ValidAudience = _settings.BundleId,       
					IssuerSigningKeys = _cachedKeys.Keys,
					ValidateLifetime = true,
					ValidateAudience = true,
					ValidateIssuer = true
				};

				var handler = new JwtSecurityTokenHandler();
				var principal = handler.ValidateToken(identityToken, parameters, out _);

				return principal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
			}
			catch (SecurityTokenExpiredException)
			{
				_logger.LogWarning("Login Apple thất bại: Token đã hết hạn.");
				return null;
			}
			catch (SecurityTokenInvalidAudienceException)
			{
				_logger.LogError($"Login Apple thất bại: Audience không khớp. Config của Server là '{_settings.BundleId}', nhưng Token lại dành cho App khác.");
				return null;
			}
			catch (SecurityTokenInvalidSignatureException)
			{
				_logger.LogError("Login Apple thất bại: Chữ ký Token không hợp lệ (Có thể token giả hoặc Key Apple đã thay đổi).");
				return null;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Lỗi không xác định khi xác thực Apple Token.");
				return null;
			}
		}
	}
}
