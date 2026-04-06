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
	public class NotificationRepository : GenericRepository<Notifications>, INotificationRepository
	{
		public NotificationRepository(DbContext context) : base(context)
		{
		}

		public async Task<(List<Notifications> Items, int TotalCount)> GetPagedNotificationForUser(Guid userId, int page, int limit)
		{
			var query = _dbSet.AsNoTracking()
				.Where(n => n.UserId == userId)
				.OrderByDescending(n => n.CreatedAt);

			var totalCount = query.Count();

			var items = await query
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (items, totalCount);
		}
	}
}
