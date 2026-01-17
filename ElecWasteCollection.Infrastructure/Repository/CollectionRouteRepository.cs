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
	public class CollectionRouteRepository : GenericRepository<CollectionRoutes>, ICollectionRouteRepository
	{
		public CollectionRouteRepository(DbContext context) : base(context)
		{
		}

		public async Task<(List<CollectionRoutes> Items, int TotalCount)> GetPagedRoutesAsync(string? collectionPointId, DateOnly? pickUpDate, string? status, int page, int limit)
		{
			var query = _dbSet.AsNoTracking()
				.AsSplitQuery()
				 .Include(r => r.Product).ThenInclude(p => p.Brand)
				 .Include(r => r.Product).ThenInclude(p => p.Category)
				 .Include(r => r.Product).ThenInclude(p => p.ProductImages)
				 .Include(r => r.Product).ThenInclude(p => p.User).ThenInclude(a => a.Posts)
				.Include(r => r.CollectionGroup)
					.ThenInclude(g => g.Shifts)
						.ThenInclude(s => s.Vehicle)

				.Include(r => r.CollectionGroup)
					.ThenInclude(g => g.Shifts)
						.ThenInclude(s => s.Collector)

				.AsQueryable();
			if (collectionPointId != null)
			{
				query = query.Where(r =>
					r.CollectionGroup != null &&
					r.CollectionGroup.Shifts != null &&
					r.CollectionGroup.Shifts.Vehicle != null &&
					r.CollectionGroup.Shifts.Vehicle.Small_Collection_Point == collectionPointId
				);
			}
			if (pickUpDate.HasValue)
			{
				query = query.Where(r => r.CollectionDate == pickUpDate.Value);
			}

			// 4. Lọc theo Status
			if (!string.IsNullOrEmpty(status))
			{
				query = query.Where(r => r.Status == status);
			}

			// 5. Đếm tổng (Chạy dưới DB)
			var totalCount = await query.CountAsync();

			// 6. Sắp xếp & Phân trang
			var items = await query
				.OrderBy(r => r.CollectionDate)
				.ThenBy(r => r.EstimatedTime)
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<List<CollectionRoutes>> GetRoutesByCollectorAndDateWithDetailsAsync(DateOnly pickUpDate,Guid collectorId)
		{
			var query = _dbSet.AsNoTracking()
				.AsSplitQuery();

			query = query
				.Include(r => r.Product).ThenInclude(p => p.Brand)
				.Include(r => r.Product).ThenInclude(p => p.Category)
				.Include(r => r.Product).ThenInclude(p => p.ProductImages)
				.Include(r => r.Product).ThenInclude(p => p.User).ThenInclude(u => u.Posts)
				.Include(r => r.Product).ThenInclude(p => p.User).ThenInclude(u => u.UserAddresses)


				.Include(r => r.CollectionGroup).ThenInclude(g => g.Shifts).ThenInclude(s => s.Vehicle)
				.Include(r => r.CollectionGroup).ThenInclude(g => g.Shifts).ThenInclude(s => s.Collector);

		
			query = query.Where(r =>
				r.CollectionGroup != null &&
				r.CollectionGroup.Shifts != null &&
				r.CollectionGroup.Shifts.CollectorId == collectorId
			);

			
			query = query.Where(r => r.CollectionDate == pickUpDate && r.CollectionGroup.Shifts.WorkDate == pickUpDate);

			return await query
				.OrderBy(r => r.EstimatedTime)
				.ToListAsync();
		}

		public async Task<List<CollectionRoutes>> GetRoutesByDateAndPointWithDetailsAsync(DateOnly pickUpDate, string collectionPointId)
		{
			var query = _dbSet.AsNoTracking()
				.AsSplitQuery();

			query = query
				.Include(r => r.Product).ThenInclude(p => p.Brand)
				.Include(r => r.Product).ThenInclude(p => p.Category)
				.Include(r => r.Product).ThenInclude(p => p.ProductImages)
				.Include(r => r.Product).ThenInclude(p => p.User).ThenInclude(u => u.Posts)
				.Include(r => r.CollectionGroup).ThenInclude(g => g.Shifts).ThenInclude(s => s.Vehicle)
				.Include(r => r.CollectionGroup).ThenInclude(g => g.Shifts).ThenInclude(s => s.Collector);



			query = query.Where(r => r.CollectionDate == pickUpDate);

			query = query.Where(r =>
				r.CollectionGroup != null &&
				r.CollectionGroup.Shifts != null &&
				r.CollectionGroup.Shifts.Vehicle != null &&
				r.CollectionGroup.Shifts.Vehicle.Small_Collection_Point == collectionPointId
			);

			return await query
				.OrderBy(r => r.EstimatedTime)
				.ToListAsync();
		}

		public async Task<List<CollectionRoutes>> GetRoutesByDateWithDetailsAsync(DateOnly pickUpDate)
		{
			var query = _dbSet.AsNoTracking()
				.AsSplitQuery();

			query = query.Where(r => r.CollectionDate == pickUpDate);


			query = query
				.Include(r => r.Product)
					.ThenInclude(p => p.Brand)
				.Include(r => r.Product)
					.ThenInclude(p => p.Category)
				.Include(r => r.Product)
					.ThenInclude(p => p.ProductImages)
				.Include(r => r.Product)
					.ThenInclude(p => p.User)
						.ThenInclude(u => u.Posts);

			query = query
				.Include(r => r.CollectionGroup)
					.ThenInclude(g => g.Shifts)
						.ThenInclude(s => s.Vehicle)
				.Include(r => r.CollectionGroup)
					.ThenInclude(g => g.Shifts)
						.ThenInclude(s => s.Collector);

			return await query
				.OrderBy(r => r.EstimatedTime)
				.ToListAsync();
		}

		public async Task<CollectionRoutes?> GetRouteWithDetailsByIdAsync(Guid collectionRouteId)
		{
			var query = _dbSet.AsNoTracking()
				.AsSplitQuery();

			query = query
				.Include(r => r.Product).ThenInclude(p => p.Brand)
				.Include(r => r.Product).ThenInclude(p => p.Category)
				.Include(r => r.Product).ThenInclude(p => p.ProductImages)
				.Include(r => r.Product).ThenInclude(p => p.User).ThenInclude(u => u.Posts)

				.Include(r => r.CollectionGroup).ThenInclude(g => g.Shifts).ThenInclude(s => s.Vehicle)
				.Include(r => r.CollectionGroup).ThenInclude(g => g.Shifts).ThenInclude(s => s.Collector);

			return await query.FirstOrDefaultAsync(r => r.CollectionRouteId == collectionRouteId);
		}
    }
}
