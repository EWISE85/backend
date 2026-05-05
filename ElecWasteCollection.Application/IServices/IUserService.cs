using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IUserService
	{
		void AddUser(User user);
		void AddRange(IEnumerable<User> newUsers);
		Task<List<UserResponse>> GetAll();
		Task<UserResponse>? GetById(Guid id);
		Task<List<UserResponse>> GetByEmail(string email);

		Task<UserProfileResponse> Profile(Guid userId);

		Task<UserResponse?> GetByPhone(string phone);
		Task<UserResponse?> GetByEmailOrPhone(string infomation);

		Task<bool> UpdateProfile(UserProfileUpdateModel model);

		Task<bool> DeleteUser(Guid accountId);

		Task<bool> BanUser(Guid userId);

		Task<PagedResultModel<UserResponse>> AdminFilterUser(AdminFilterUserModel model);

		Task<bool> UpdatePointForUser(Guid userId, double pointToAdd);
		Task<UserPointModel> GetPointByUserId(Guid userId);
		Task<PagedResultModel<UserResponse>> FilterUserByRadius(string CollectionUnitId, double km, int page = 1, int limit = 10);
		Task<PagedResultModel<UserResponse>> FilterUserByPoint(double minPoint, double maxPoint, int page = 1, int limit = 10);

	}
}
