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

		public async Task<List<User>> AdminFilterUser(int page, int limit, DateOnly? fromDate, DateOnly? toDate, string? email, string? status)
		{
			var query = _dbSet.AsNoTracking();
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
				var from = fromDate.Value.ToDateTime(TimeOnly.MinValue);
				query = query.Where(u => u.CreateAt >= from);
			}

			if (toDate.HasValue)
			{
				var to = toDate.Value.ToDateTime(TimeOnly.MaxValue);
				query = query.Where(u => u.CreateAt <= to);
			}

		
			query = query.OrderByDescending(u => u.CreateAt);

		
			var result = await query
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return result;
		}
	}
}
