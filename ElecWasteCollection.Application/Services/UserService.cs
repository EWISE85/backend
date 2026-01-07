using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class UserService : IUserService
	{
		private readonly IFirebaseService _firebaseService;
		private readonly ITokenService _tokenService;
		private readonly IUserRepository _userRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserPointService _userPointService;
		private readonly IUserPointRepository _userPointRepository;

		public UserService(IFirebaseService firebaseService, ITokenService tokenService, IUserRepository userRepository, IUnitOfWork unitOfWork, IUserPointService userPointService, IUserPointRepository userPointRepository)
		{
			_firebaseService = firebaseService;
			_tokenService = tokenService;
			_userRepository = userRepository;
			_unitOfWork = unitOfWork;
			_userPointService = userPointService;
			_userPointRepository = userPointRepository;
		}

		public async Task<List<UserResponse>> GetAll()
		{
			var users = await _userRepository.GetsAsync(u => u.Role == UserRole.User.ToString());
			if (users == null || users.Count == 0)
			{
				return new List<UserResponse>();
			}
			var userResponses = users.Select(u => new UserResponse
			{
				UserId = u.UserId,
				Name = u.Name,
				Email = u.Email,
				Phone = u.Phone,
				Avatar = u.Avatar,
				Role = u.Role,
				SmallCollectionPointId = u.SmallCollectionPointId
			}).ToList();
			return userResponses;
		}

		public void AddRange(IEnumerable<User> newUsers)
		{
			throw new NotImplementedException();
		}

		public async void AddUser(User user)
		{
			var existingEmail = _userRepository.GetAsync(u => u.Email == user.Email);
			if (existingEmail != null) throw new AppException("Email đã được liên kết với tài khoản khác, vui lòng thử email khác", 400);
			var repo = _unitOfWork.Users;
			await repo.AddAsync(user);
			await _unitOfWork.SaveAsync();
		}

		public async Task<UserResponse>? GetById(Guid id)
		{
			var user = await _userRepository.GetAsync(u => u.UserId == id);
			if (user == null) throw new AppException("User không tồn tại", 404);
			var userResponse = new UserResponse
			{
				UserId = user.UserId,
				Name = user.Name,
				Email = user.Email,
				Phone = user.Phone,
				Avatar = user.Avatar,
				Role = user.Role,
				SmallCollectionPointId = user.SmallCollectionPointId,
				CollectionCompanyId = user.CollectionCompanyId
			};
			return userResponse;
		}

		//public void UpdateUser(int iat, int ing, Guid id)
		//{
		//	var user = users.FirstOrDefault(u => u.UserId == id);
		//	if (user != null)
		//	{
		//		user.Iat = iat;
		//		user.Ing = ing;
		//	}
		//}

		

		public async Task<UserProfileResponse> Profile(Guid userId)
		{
			var user = await _userRepository.GetAsync(u => u.UserId == userId);
			if (user == null) throw new AppException("User không tồn tại", 404);
			var points = await _userPointRepository.GetAsync(p => p.UserId == user.UserId);
			var pointsValue = points == null ? 0 : points.Points;
			//UserSettingsModel settingsObj;
			//if (string.IsNullOrEmpty(user.Preferences))
			//{
			//	settingsObj = new UserSettingsModel { ShowMap = false };
			//}
			//else
			//{
			//	try
			//	{
			//		settingsObj = JsonSerializer.Deserialize<UserSettingsModel>(user.Preferences)?? new UserSettingsModel { ShowMap = false };
			//	}
			//	catch
			//	{
			//		settingsObj = new UserSettingsModel { ShowMap = false };
			//	}
			//}
			var userProfile = new UserProfileResponse
			{
				UserId = user.UserId,
				Name = user.Name,
				Email = user.Email,
				Phone = user.Phone,
				Avatar = user.Avatar,
				Role = user.Role,
				Points = pointsValue,
				CollectionCompanyId = user.CollectionCompanyId,
				SmallCollectionPointId = user.SmallCollectionPointId,
				//Settings = settingsObj
			};
			return userProfile;
		}

		public async Task<UserResponse?> GetByPhone(string phone)
		{
			var user = await _userRepository.GetAsync(u => u.Phone == phone);
			if (user == null) throw new AppException("User không tồn tại", 404);
			var userResponse = new UserResponse
			{
				UserId = user.UserId,
				Name = user.Name,
				Email = user.Email,
				Phone = user.Phone,
				Avatar = user.Avatar,
				Role = user.Role,
				SmallCollectionPointId = user.SmallCollectionPointId
			};
			return userResponse;
		}

		public async Task<bool> UpdateProfile(UserProfileUpdateModel model)
		{
			var user = await _userRepository.GetAsync(u => u.UserId == model.UserId);
			if (user == null) throw new AppException("User không tồn tại", 404);
			user.Email = model.Email ?? user.Email;
			user.Avatar = model.AvatarUrl ?? user.Avatar;
			user.Phone = model.phoneNumber ?? user.Phone;
			//user.Preferences = JsonSerializer.Serialize(model.Settings);
			_unitOfWork.Users.Update(user);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<bool> DeleteUser(Guid userId)
		{
			var user = await _userRepository.GetAsync(u => u.UserId == userId);
			if (user == null) throw new AppException("User không tồn tại", 404);
			user.Status = UserStatus.Inactive.ToString();
			user.AppleId = null;
			user.Email = null;
			_unitOfWork.Users.Update(user);
			await _unitOfWork.SaveAsync();
			return true;
		}
	}
}
