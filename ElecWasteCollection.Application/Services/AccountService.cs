using DocumentFormat.OpenXml.Spreadsheet;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
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
		private readonly IAppleAuthService _appleAuthService;

		public AccountService(IUnitOfWork unitOfWork, IAccountRepsitory accountRepository, IFirebaseService firebaseService, ITokenService tokenService, IUserRepository userRepository, IAppleAuthService appleAuthService)
		{
			_unitOfWork = unitOfWork;
			_accountRepository = accountRepository;
			_firebaseService = firebaseService;
			_tokenService = tokenService;
			_userRepository = userRepository;
			_appleAuthService = appleAuthService;
		}
		public async Task<bool> AddNewAccount(Account account)
		{
			var repository = _unitOfWork.Accounts;
			await repository.AddAsync(account);
			await _unitOfWork.SaveAsync();
			return true;
		}
		public async Task<LoginResponseModel> LoginWithGoogleAsync(string token)
		{
			var decodedToken = await _firebaseService.VerifyIdTokenAsync(token);
			var email = decodedToken.Claims["email"].ToString();
			if (email == null) throw new Exception("Không lấy được email từ trong token firebase");
			string name = decodedToken.Claims.ContainsKey("name") ? decodedToken.Claims["name"].ToString() : email;
			string picture = decodedToken.Claims.ContainsKey("picture") ? decodedToken.Claims["picture"].ToString() : null;
			var user = await _userRepository.GetAsync(u => u.Email == email && u.Status == UserStatus.Active.ToString());
			if (user == null)
			{
				var defaultSettings = new UserSettingsModel
				{
					ShowMap = false
				};
				user = new User
				{
					UserId = Guid.NewGuid(),
					Email = email,
					Name = name,
					Avatar = picture,
					Role = UserRole.User.ToString(),
					Preferences = JsonSerializer.Serialize(defaultSettings),
					Status = UserStatus.Active.ToString()
				};
				var point = new UserPoints
				{
					UserPointId = Guid.NewGuid(),
					UserId = user.UserId,
					Points = 0
				};
				var repo = _unitOfWork.Users;
				await repo.AddAsync(user);
				await _unitOfWork.UserPoints.AddAsync(point);
				await _unitOfWork.SaveAsync();
			}
			var accessToken = await _tokenService.GenerateToken(user);
			var loginResponse = new LoginResponseModel
			{
				AccessToken = accessToken,
				IsFirstLogin = false
			};
			return loginResponse;
		}

		public async Task<LoginResponseModel> Login(string userName, string password)
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
			if (user.Status != UserStatus.Active.ToString())
			{
				throw new AppException("Tài khoản đã bị khóa", 403);
			}
			var accessToken = await _tokenService.GenerateToken(user);
			var loginResponse = new LoginResponseModel
			{
				AccessToken = accessToken,
				IsFirstLogin = account.IsFirstLogin
			};
			return loginResponse;
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

		public async Task<LoginResponseModel> LoginWithAppleAsync(string identityToken, string? firstName, string? lastName)
		{
			var appleUser = await _appleAuthService.ValidateTokenAndGetAppleInfoAsync(identityToken);
			if (appleUser == null)
			{
				throw new AppException("Apple Token không hợp lệ!", 400);
			}


			var user = await _userRepository.GetAsync(u => u.AppleId == appleUser.AppleId && u.Status == UserStatus.Active.ToString());

			if (user == null)
			{
				
				if (!string.IsNullOrEmpty(appleUser.Email))
				{
					user = await _userRepository.GetAsync(u => u.Email == appleUser.Email && u.Status == UserStatus.Active.ToString());
				}

				if (user != null)
				{
					
					user.AppleId = appleUser.AppleId;

					

					_unitOfWork.Users.Update(user);
					await _unitOfWork.SaveAsync();
				}
				else
				{
					var defaultSettings = new UserSettingsModel
					{
						ShowMap = false
					};

					string displayName = (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
										 ? (firstName + " " + lastName).Trim()
										 : (appleUser.Email ?? "Apple User");

					user = new User
					{
						UserId = Guid.NewGuid(),
						AppleId = appleUser.AppleId,
						Email = appleUser.Email, 
						Phone = null,
						Name = displayName,
						Avatar = null,
						CreateAt = DateTime.UtcNow,
						Role = UserRole.User.ToString(),
						Preferences = JsonSerializer.Serialize(defaultSettings),
						Status = UserStatus.Active.ToString()
					};

					var point = new UserPoints
					{
						UserPointId = Guid.NewGuid(),
						UserId = user.UserId,
						Points = 0
					};

					await _unitOfWork.Users.AddAsync(user);
					await _unitOfWork.UserPoints.AddAsync(point);
					await _unitOfWork.SaveAsync();
				}
			}

			var accessToken = await _tokenService.GenerateToken(user);

			var loginResponse = new LoginResponseModel
			{
				AccessToken = accessToken,
				
			};

			return loginResponse;
		}
	}
}
