using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface IUserRepository : IGenericRepository<User>
	{
		Task<(List<User> Users, int TotalCount)> AdminFilterUser(int page, int limit, DateOnly? fromDate, DateOnly? toDate, string? email, string? status);
		Task<(List<User> Items, int TotalItems)> GetUsersByPointRangeAsync(
			double minPoint,
			double maxPoint,
			int page,
			int limit);
		Task<(List<User> Items, int TotalItems)> GetUsersByRadiusAsync(double warehouseLat, double warehouseLng, double radiusKm, int page, int limit);
	}
}
