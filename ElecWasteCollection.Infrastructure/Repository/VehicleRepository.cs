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
	public class VehicleRepository : GenericRepository<Vehicles>, IVehicleRepository
	{
		public VehicleRepository(DbContext context) : base(context)
		{
		}

		public async Task<(List<Vehicles> Items, int TotalCount)> GetPagedVehiclesAsync(
			string? collectionCompanyId,
			string? smallCollectionPointId,
			string? plateNumber,
			string? status,
			int page,
			int limit)
		{
			var query = _dbSet.AsNoTracking();

			// 1. Lọc theo Company (Logic bắc cầu)
			if (!string.IsNullOrEmpty(collectionCompanyId))
			{
				// Giả sử logic là check bảng SmallCollectionPoints
				query = query.Where(v => _context.Set<SmallCollectionPoints>()
					.Any(scp => scp.SmallCollectionPointsId == v.Small_Collection_Point &&
								scp.CompanyId.ToString() == collectionCompanyId));
			}

			// 2. Lọc theo SmallCollectionPointId
			if (!string.IsNullOrEmpty(smallCollectionPointId))
			{
				query = query.Where(v => v.Small_Collection_Point == smallCollectionPointId);
			}

			// 3. Lọc theo Biển số
			if (!string.IsNullOrEmpty(plateNumber))
			{
				query = query.Where(v => v.Plate_Number.Contains(plateNumber));
			}

			// 4. Lọc theo Trạng thái
			if (!string.IsNullOrEmpty(status))
			{
				var trimmedStatus = status.Trim().ToLower();
				query = query.Where(p => !string.IsNullOrEmpty(p.Status) && p.Status.ToLower() == trimmedStatus);
			}

			// Đếm tổng
			var totalCount = await query.CountAsync();

			// Phân trang
			var items = await query
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (items, totalCount);
		}
	}
}
