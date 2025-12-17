using DocumentFormat.OpenXml.Spreadsheet;
using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class AccountService : IAccountService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAccountRepsitory _accountRepository;
		private readonly IFirebaseService _firebaseService;
		private readonly ITokenService _tokenService;
		private readonly IUserRepository _userRepository;
		public AccountService(IUnitOfWork unitOfWork, IAccountRepsitory accountRepository, IFirebaseService firebaseService, ITokenService tokenService, IUserRepository userRepository)
		{
			_unitOfWork = unitOfWork;
			_accountRepository = accountRepository;
			_firebaseService = firebaseService;
			_tokenService = tokenService;
			_userRepository = userRepository;
		}
		public async Task<bool> AddNewAccount(Account account)
		{
			var repository = _unitOfWork.Accounts;
			await repository.AddAsync(account);
			await _unitOfWork.SaveAsync();
			return true;
		}
		public async Task<string> LoginWithGoogleAsync(string token)
		{
			var decodedToken = await _firebaseService.VerifyIdTokenAsync(token);
			var email = decodedToken.Claims["email"].ToString();
			if (email == null) throw new Exception("Không lấy được email từ trong token firebase");
			string name = decodedToken.Claims.ContainsKey("name") ? decodedToken.Claims["name"].ToString() : email;
			string picture = decodedToken.Claims.ContainsKey("picture") ? decodedToken.Claims["picture"].ToString() : null;
			var user = await _userRepository.GetAsync(u => u.Email == email);
			if (user == null)
			{
				user = new User
				{
					UserId = Guid.NewGuid(),
					Email = email,
					Name = name,
					Avatar = picture,
					Role = UserRole.User.ToString(),
				};
				var repo = _unitOfWork.Users;
				await repo.AddAsync(user);
				await _unitOfWork.SaveAsync();
			}
			var accessToken = await _tokenService.GenerateToken(user);
			return accessToken;
		}

		public async Task<string> Login(string userName, string password)
		{
			var account = await _accountRepository.GetAsync(u => u.Username == userName && u.PasswordHash == password);
			if (account == null)
			{
				throw new AppException("Tài khoản không tồn tại", 404);
			}
			var user = await _userRepository.GetAsync(u => u.UserId == account.UserId);
			if (user == null)
			{
				throw new AppException("User không tồn tại", 404);
			}
			var accessToken = await _tokenService.GenerateToken(user);
			return accessToken;
		}

		public async Task<bool> ChangePassword(string email, string newPassword, string confirmPassword)
		{
			var user = await _userRepository.GetAsync(u => u.Email == email);
			if (user == null)
			{
				throw new AppException("User không tồn tại", 404);
			}
			if (newPassword != confirmPassword)
			{
				throw new AppException("Mật khẩu xác nhận không khớp", 400);
			}
			var account = await _accountRepository.GetAsync(a => a.UserId == user.UserId);
			if (account == null)
			{
				throw new AppException("Tài khoản không tồn tại", 404);
			}
			account.PasswordHash = newPassword;
			account.IsFirstLogin = false;
			_unitOfWork.Accounts.Update(account);
			await _unitOfWork.SaveAsync();
			return true;
		}
	}
}
