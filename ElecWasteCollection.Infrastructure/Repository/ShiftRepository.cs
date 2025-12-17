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
	public class ShiftRepository : GenericRepository<Shifts>, IShiftRepository
	{
		public ShiftRepository(DbContext context) : base(context)
		{
		}

		public async Task<(List<Shifts> Items, int TotalCount)> GetPagedShiftsAsync(string? collectionCompanyId, string? smallCollectionPointId, string? status, DateOnly? fromDate, DateOnly? toDate, int page, int limit)
		{
			var query = _dbSet.AsNoTracking()
				.Include(s => s.Collector)
				.Include(s => s.Vehicle)
				.AsQueryable();
			if (collectionCompanyId != null)
			{
				query = query.Where(s => s.Collector.CollectionCompanyId == collectionCompanyId);
			}
			if (smallCollectionPointId != null)
			{
				query = query.Where(s => s.Collector.SmallCollectionPointId == smallCollectionPointId);
			}

			if (!string.IsNullOrEmpty(status))
			{
				query = query.Where(s => s.Status == status);
			}

			if (fromDate.HasValue)
			{
				query = query.Where(s => s.WorkDate >= fromDate.Value);
			}
			if (toDate.HasValue)
			{
				query = query.Where(s => s.WorkDate <= toDate.Value);
			}

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(s => s.WorkDate)
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<Shifts?> GetShiftWithDetailsAsync(string shiftId)
		{
			return await _dbSet
		.Include(s => s.Collector) 
		.Include(s => s.Vehicle)  
		.AsNoTracking()           
		.FirstOrDefaultAsync(s => s.ShiftId == shiftId);
		}
	}
}
