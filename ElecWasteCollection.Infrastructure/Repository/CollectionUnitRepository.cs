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
	public class CollectionUnitRepository : GenericRepository<CollectionUnit>, ICollectionUnitRepository
    {
		public CollectionUnitRepository(DbContext context) : base(context)
		{
		}

		public async Task<(List<CollectionUnit> Items, int TotalCount)> GetPagedAsync(string? companyId, string? status, int page, int limit)
		{
			var query = _dbSet.AsNoTracking();

			if (companyId != null)
			{
				query = query.Where(s => s.CompanyId == companyId);
			}

			if (!string.IsNullOrEmpty(status))
			{
				var trimmedStatus = status.Trim().ToLower();
				query = query.Where(p => !string.IsNullOrEmpty(p.Status) && p.Status.ToLower() == trimmedStatus);
			}

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(s => s.CollectionUnitId)
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (items, totalCount);
		}
        public async Task<string?> GetScpNameAsync(string scpId)
        {
            return await _dbSet.AsNoTracking()
                .Where(s => s.CollectionUnitId == scpId)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();
        }
    }
}
