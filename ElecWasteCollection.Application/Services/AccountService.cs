using DocumentFormat.OpenXml.Spreadsheet;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.Tokens;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Google.Apis.Auth.OAuth2.Requests;
using Google.Apis.Auth.OAuth2.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
			var user = await _userRepository.GetAsync(u => u.Email == email && u.Status == UserStatus.DANG_HOAT_DONG.ToString());
			if (user == null)
			{
				var defaultSettings = new UserSettingsModel
				{
					ShowMap = false
				};
				var role = await _unitOfWork.Roles.GetAsync(r => r.Name == UserRole.User.ToString());
				user = new User
				{
					UserId = Guid.NewGuid(),
					Email = email,
					Name = name,
					Avatar = picture,
					RoleId = role.RoleId,
					CreateAt = DateTime.UtcNow,
					Points = 0,
					Status = UserStatus.DANG_HOAT_DONG.ToString()
				};
				
				var repo = _unitOfWork.Users;
				await repo.AddAsync(user);
			}
			var oldTokens = await _unitOfWork.UserTokens.GetsAsync(t => t.UserId == user.UserId);
			foreach (var ot in oldTokens) _unitOfWork.UserTokens.Delete(ot);

			var accessToken = await _tokenService.GenerateToken(user);
			var refreshTokenString = _tokenService.GenerateRefreshTokenString(); 

			await _unitOfWork.UserTokens.AddAsync(new UserToken
			{
				UserTokenId = Guid.NewGuid(),
				Token = refreshTokenString,
				AccessToken = accessToken, 
				UserId = user.UserId,
				ExpiryDate = DateTime.UtcNow.AddDays(7),
				CreatedAt = DateTime.UtcNow
			});
			await _unitOfWork.SaveAsync();
			var loginResponse = new LoginResponseModel
			{
				AccessToken = accessToken,
				RefreshToken = refreshTokenString,
				IsFirstLogin = false
			};
			return loginResponse;
		}

		public async Task<LoginResponseModel> Login(string userName, string password)
		{
			var account = await _accountRepository.GetAsync(u => u.Username == userName);
			if (account == null)
			{
				throw new AppException("Tài khoản hoặc mật khẩu không chính xác", 400);
			}

			bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, account.PasswordHash);
			if (!isPasswordValid)
			{
				throw new AppException("Tài khoản hoặc mật khẩu không chính xác", 400);
			}

			var user = await _userRepository.GetAsync(u => u.UserId == account.UserId);
			if (user == null)
			{
				throw new AppException("User không tồn tại", 404);
			}
			if (user.Status != UserStatus.DANG_HOAT_DONG.ToString())
			{
				throw new AppException("Tài khoản đã bị khóa", 403);
			}
			var oldTokens = await _unitOfWork.UserTokens.GetsAsync(t => t.UserId == user.UserId);
			if (oldTokens != null && oldTokens.Any())
			{
				foreach (var oldToken in oldTokens)
				{
					_unitOfWork.UserTokens.Delete(oldToken);
				}
			}
			var oldDeviceTokens = await _unitOfWork.UserDeviceTokens.GetsAsync(t => t.UserId == user.UserId);
			if (oldDeviceTokens != null && oldDeviceTokens.Any())
			{
				foreach (var oldDevice in oldDeviceTokens)
				{
					_unitOfWork.UserDeviceTokens.Delete(oldDevice);
				}
			}

			var accessToken = await _tokenService.GenerateToken(user);
			var refreshTokenString = _tokenService.GenerateRefreshTokenString();

			var newUserToken = new UserToken
			{
				UserTokenId = Guid.NewGuid(),
				Token = refreshTokenString,
				AccessToken = accessToken,
				UserId = user.UserId,
				ExpiryDate = DateTime.UtcNow.AddDays(7),
				CreatedAt = DateTime.UtcNow
			};

			await _unitOfWork.UserTokens.AddAsync(newUserToken);
			await _unitOfWork.SaveAsync();

			var loginResponse = new LoginResponseModel
			{
				AccessToken = accessToken,
				RefreshToken = refreshTokenString,
				IsFirstLogin = account.IsFirstLogin
			};

			return loginResponse;
		}

		public async Task<bool> ChangePassword(string email, string newPassword, string confirmPassword)
		{
			var user = await _userRepository.GetAsync(u => u.Email == email);
			Console.WriteLine($"Id: {user.UserId}, User: {(user != null ? user.Email : "null")}");
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
			account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
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
			var roleUser = await _unitOfWork.Roles.GetAsync(r => r.Name == UserRole.User.ToString());
			if (roleUser == null) throw new AppException("Hệ thống chưa cấu hình Role User!", 500);
			var user = await _userRepository.GetAsync(u => u.AppleId == appleUser.AppleId && u.Status == UserStatus.DANG_HOAT_DONG.ToString());

			if (user == null)
			{
				if (!string.IsNullOrEmpty(appleUser.Email))
				{
					user = await _userRepository.GetAsync(u => u.Email == appleUser.Email && u.Status == UserStatus.DANG_HOAT_DONG.ToString());
					if (user != null && user.RoleId != roleUser.RoleId)
					{
						throw new AppException("Email của appleId này đã tồn tại với một tài khoản khác!", 400);
					}
				}

				if (user != null)
				{
					user.AppleId = appleUser.AppleId;
					_unitOfWork.Users.Update(user);
				}
				else
				{
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
						RoleId = roleUser.RoleId,
						Points = 0,
						Status = UserStatus.DANG_HOAT_DONG.ToString()
					};

					await _unitOfWork.Users.AddAsync(user);
				}
			}

			var oldTokens = await _unitOfWork.UserTokens.GetsAsync(t => t.UserId == user.UserId);
			foreach (var ot in oldTokens) _unitOfWork.UserTokens.Delete(ot);

			var oldDeviceTokens = await _unitOfWork.UserDeviceTokens.GetsAsync(t => t.UserId == user.UserId);
			foreach (var odt in oldDeviceTokens) _unitOfWork.UserDeviceTokens.Delete(odt);

			var accessToken = await _tokenService.GenerateToken(user);
			var refreshTokenString = _tokenService.GenerateRefreshTokenString();

			await _unitOfWork.UserTokens.AddAsync(new UserToken
			{
				UserTokenId = Guid.NewGuid(),
				Token = refreshTokenString,
				AccessToken = accessToken,
				UserId = user.UserId,
				ExpiryDate = DateTime.UtcNow.AddDays(7),
				CreatedAt = DateTime.UtcNow
			});

			await _unitOfWork.SaveAsync();

			var loginResponse = new LoginResponseModel
			{
				AccessToken = accessToken,
				RefreshToken = refreshTokenString,
				IsFirstLogin = false
			};

			return loginResponse;
		}
		public async Task<LoginResponseModel?> RefreshTokenAsync(RefreshTokenModel request)
		{
			var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
			if (principal == null) return null;

			var userIdStr = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
			if (!Guid.TryParse(userIdStr, out Guid userId)) return null;

			var storedToken = await _unitOfWork.UserTokens.GetAsync(
				t => t.Token == request.RefreshToken && t.UserId == userId);

			if (storedToken == null || storedToken.ExpiryDate <= DateTime.UtcNow)
			{
				return null; 
			}

			_unitOfWork.UserTokens.Delete(storedToken);

			var user = await _unitOfWork.Users.GetByIdAsync(userId);
			if (user == null) throw new AppException("Không tìm thấy người dùng",404);
			var newAccessToken = await _tokenService.GenerateToken(user);
			var newRefreshToken = _tokenService.GenerateRefreshTokenString(); 

			await _unitOfWork.UserTokens.AddAsync(new UserToken
			{
				UserTokenId = Guid.NewGuid(),
				Token = newRefreshToken,
				AccessToken = newAccessToken,
				ExpiryDate = DateTime.UtcNow.AddDays(7),
				UserId = userId,
				CreatedAt = DateTime.UtcNow
			});

			await _unitOfWork.SaveAsync();

			return new LoginResponseModel
			{
				AccessToken = newAccessToken,
				RefreshToken = newRefreshToken,
				IsFirstLogin = false
			};
		}
		public async Task<bool> LogoutAsync(Guid userId)
		{
			var userTokens = await _unitOfWork.UserTokens.GetsAsync(x => x.UserId == userId);
			if (userTokens != null && userTokens.Any())
			{
				foreach (var token in userTokens)
				{
					_unitOfWork.UserTokens.Delete(token);
				}
			}

			var deviceTokens = await _unitOfWork.UserDeviceTokens.GetsAsync(x => x.UserId == userId);
			if (deviceTokens != null && deviceTokens.Any())
			{
				foreach (var device in deviceTokens)
				{
					_unitOfWork.UserDeviceTokens.Delete(device);
				}
			}

			// Lưu tất cả thay đổi xuống Database
			await _unitOfWork.SaveAsync();

			return true;
		}
	}
}
