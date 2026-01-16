using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
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

			var secretKey = jwtSettings["SecretKey"];
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
				// Truyền cái List claims vừa tạo vào đây
				Subject = new ClaimsIdentity(claims),

				Expires = DateTime.UtcNow.AddMonths(10),
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
	}
}
