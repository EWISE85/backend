using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class TokenService : ITokenService
	{
		private readonly IConfiguration _configuration;
		public TokenService(IConfiguration configuration)
		{
			_configuration = configuration;
		}
		public Task<string> GenerateToken(User user)
		{
			var jwtTokenHandler = new JwtSecurityTokenHandler();
			var jwtSettings = _configuration.GetSection("Jwt");

			var secretKey = jwtSettings["SecretKey"]; // Lưu ý tên key ở đây
			var issuer = jwtSettings["Issuer"];
			var audience = jwtSettings["Audience"];
			var keyBytes = Encoding.UTF8.GetBytes(secretKey);

			var claims = new List<Claim>
	{
		new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
		new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
	};

			if (!string.IsNullOrEmpty(user.Email))
			{
				claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
			}
			else if (!string.IsNullOrEmpty(user.AppleId))
			{
				claims.Add(new Claim("apple_id", user.AppleId));
			}

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(claims),
				Expires = DateTime.UtcNow.AddMinutes(1),
				SigningCredentials = new SigningCredentials(
					new SymmetricSecurityKey(keyBytes),
					SecurityAlgorithms.HmacSha256Signature
				),
				Issuer = issuer,
				Audience = audience
			};

			var token = jwtTokenHandler.CreateToken(tokenDescriptor);
			var tokenString = jwtTokenHandler.WriteToken(token);

			return Task.FromResult(tokenString);
		}

		public ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
		{
			if (string.IsNullOrEmpty(token)) return null;

			var jwtSettings = _configuration.GetSection("Jwt");
			var secretKey = jwtSettings["SecretKey"];

			var tokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
				ValidateIssuer = false, 
				ValidateAudience = false, 
				ValidateLifetime = false 
			};

			var tokenHandler = new JwtSecurityTokenHandler();

			try
			{
				var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

				if (securityToken is not JwtSecurityToken jwtSecurityToken ||
					!jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
				{
					return null;
				}

				return principal;
			}
			catch (Exception)
			{
				return null;
			}
		}
		public string GenerateRefreshTokenString()
		{
			var randomNumber = new byte[64];
			using var rng = RandomNumberGenerator.Create();
			rng.GetBytes(randomNumber);
			return Convert.ToBase64String(randomNumber);
		}
	}
}
