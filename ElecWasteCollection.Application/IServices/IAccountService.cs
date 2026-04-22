using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.Tokens;
using ElecWasteCollection.Domain.Entities;


namespace ElecWasteCollection.Application.IServices
{
	public interface IAccountService
	{
		Task<bool> AddNewAccount(Account account);

		Task<LoginResponseModel> LoginWithGoogleAsync(string token);

		Task<LoginResponseModel> Login(string userName, string password);

		Task<bool> ChangePassword(string email, string newPassword, string confirmPassword);

		Task<LoginResponseModel> LoginWithAppleAsync(string identityToken, string? firstName, string? lastName);
		Task<LoginResponseModel?> RefreshTokenAsync(RefreshTokenModel request);
		Task<bool> LogoutAsync(Guid userId);
	}
}
