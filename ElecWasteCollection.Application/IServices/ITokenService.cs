using ElecWasteCollection.Domain.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ElecWasteCollection.Application.IServices
{
	public interface ITokenService
	{
		Task<string> GenerateToken(User user);
		ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token);
		string GenerateRefreshTokenString();
	}
}
