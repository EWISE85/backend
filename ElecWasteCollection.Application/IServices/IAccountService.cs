using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IAccountService
	{
		Task<bool> AddNewAccount(Account account);

		Task<string> LoginWithGoogleAsync(string token);

		Task<LoginResponseModel> Login(string userName, string password);

		Task<bool> ChangePassword(string email, string newPassword, string confirmPassword);

		Task<LoginResponseModel> LoginWithAppleAsync(string identityToken, string? firstName, string? lastName);

	}
}
