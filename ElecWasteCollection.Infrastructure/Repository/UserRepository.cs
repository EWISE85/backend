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
	}
}
