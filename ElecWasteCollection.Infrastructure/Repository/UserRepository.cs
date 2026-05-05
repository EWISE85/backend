using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.Repository
{
	public class UserRepository : GenericRepository<User>, IUserRepository
	{
		public UserRepository(DbContext context) : base(context)
		{
		}

		// Thay đổi kiểu trả về thành Tuple: (List<User> Users, int TotalCount)
		public async Task<(List<User> Users, int TotalCount)> AdminFilterUser(int page, int limit, DateOnly? fromDate, DateOnly? toDate, string? email, string? status)
		{
			var query = _dbSet.AsNoTracking();
			query = query.Include(u => u.Role).AsQueryable();

			// --- 1. Áp dụng các bộ lọc (Filter) ---
			if (!string.IsNullOrEmpty(email))
			{
				var searchEmail = email.Trim();
				query = query.Where(u => u.Email != null && u.Email.Contains(searchEmail));
			}

			if (!string.IsNullOrEmpty(status))
			{
				query = query.Where(u => u.Status == status);
			}

			if (fromDate.HasValue)
			{

				var from = DateTime.SpecifyKind(fromDate.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

				query = query.Where(u => u.CreateAt >= from);
			}

			if (toDate.HasValue)
			{

				var to = DateTime.SpecifyKind(toDate.Value.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

				query = query.Where(u => u.CreateAt <= to);
			}


			var totalCount = await query.CountAsync();

			query = query.OrderByDescending(u => u.CreateAt);

			var users = await query
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (users, totalCount);
		}
		public Task<(List<User> Items, int TotalItems)> GetUsersByPointRangeAsync(double minPoint, double maxPoint, int page, int limit)
		{
			var query = _dbSet.AsNoTracking()
				.Include(u => u.Role)
				.Where(u => u.Status == UserStatus.DANG_HOAT_DONG.ToString() && u.Points >= minPoint && u.Points <= maxPoint);

			var totalItems = query.Count();

			var items = query
				.OrderByDescending(u => u.CreateAt)
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToList();

			return Task.FromResult((items, totalItems));
		}

		public async Task<(List<User> Items, int TotalItems)> GetUsersByRadiusAsync(double warehouseLat, double warehouseLng, double radiusKm, int page, int limit)
		{
			double latDelta = radiusKm / 111.0;
			double lngDelta = radiusKm / (111.0 * Math.Cos(warehouseLat * Math.PI / 180.0));

			double minLat = warehouseLat - latDelta;
			double maxLat = warehouseLat + latDelta;
			double minLng = warehouseLng - lngDelta;
			double maxLng = warehouseLng + lngDelta;

			var potentialUsers = await _dbSet.AsNoTracking()
								.Include(u => u.Role)
								.Include(u => u.UserAddresses.Where(a => a.isDefault == true))
								.Where(u => u.Status == UserStatus.DANG_HOAT_DONG.ToString())
								.Where(u => u.UserAddresses.Any(a =>
										a.isDefault == true &&
										a.Iat.HasValue && a.Ing.HasValue &&
										a.Iat.Value >= minLat && a.Iat.Value <= maxLat &&
										a.Ing.Value >= minLng && a.Ing.Value <= maxLng))
								.ToListAsync();

			var validUsers = new List<User>();

			foreach (var user in potentialUsers)
			{
				var defaultAddress = user.UserAddresses.FirstOrDefault(a => a.isDefault);
				if (defaultAddress != null && defaultAddress.Iat.HasValue && defaultAddress.Ing.HasValue)
				{
					double distance = GeoHelper.DistanceKm(
						warehouseLat, warehouseLng,
						defaultAddress.Iat.Value, defaultAddress.Ing.Value);

					if (distance <= radiusKm)
					{
						validUsers.Add(user);
					}
				}
			}

			int totalItems = validUsers.Count;

			var pagedItems = validUsers
				.OrderByDescending(u => u.CreateAt)
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToList();
			return (pagedItems, totalItems);
		}
	}
}
