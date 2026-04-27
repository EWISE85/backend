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
	public class CollectorRepository : GenericRepository<User>, ICollectorRepository
	{
		public CollectorRepository(DbContext context) : base(context)
		{
		}

		public async Task<(List<User> Items, int TotalCount)> GetPagedCollectorsAsync(string? status, string? companyId, string? smallCollectionPointId, int page, int limit)
		{
			var query = _dbSet.AsNoTracking();
			query = query.Include(u => u.Role).AsQueryable();
			query = query.Where(u => u.Role.Name == UserRole.Collector.ToString());
			query = query.Include(u => u.CollectionUnits);

			if (!string.IsNullOrEmpty(status))
			{
				var trimmedStatus = status.Trim().ToLower();
				query = query.Where(p => !string.IsNullOrEmpty(p.Status) && p.Status.ToLower() == trimmedStatus);
			}

			if (companyId != null)
			{
				query = query.Where(u => u.CompanyId == companyId);
			}

			if (smallCollectionPointId != null)
			{
				query = query.Where(u => u.CollectionUnitId == smallCollectionPointId);
			}

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(u => u.UserId) 
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (items, totalCount);
		}
	}
}
