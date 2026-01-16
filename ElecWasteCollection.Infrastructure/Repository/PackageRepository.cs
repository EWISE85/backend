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
	public class PackageRepository : GenericRepository<Packages>, IPackageRepository
	{
		public PackageRepository(DbContext context) : base(context)
		{
		}
		public async Task<(List<Packages> Items, int TotalCount)> GetPagedPackagesWithDetailsAsync(
			string? smallCollectionPointsId,
			string? status,
			int page,
			int limit)
		{
			var query = _dbSet.AsNoTracking().AsSplitQuery();

			
			query = query
				.Include(p => p.Products)
					.ThenInclude(pr => pr.Brand)
				.Include(p => p.Products)
					.ThenInclude(pr => pr.Category);

			if (smallCollectionPointsId != null)
			{
				query = query.Where(p => p.SmallCollectionPointsId == smallCollectionPointsId);
			}

			if (!string.IsNullOrEmpty(status))
			{
				var trimmedStatus = status.Trim().ToLower();
				query = query.Where(p => !string.IsNullOrEmpty(p.Status) && p.Status.ToLower() == trimmedStatus);
			}

			var totalCount = await query.CountAsync();

			var pagedPackages = await query
				.OrderByDescending(p => p.CreateAt)
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (pagedPackages, totalCount);
		}

		public async Task<(List<Packages> Items, int TotalCount)> GetPagedPackagesWithDetailsByRecyclerAsync(
	string? recyclerId,
	string? status,
	int page,
	int limit)
		{
			var query = _dbSet.AsNoTracking().AsSplitQuery();

			query = query
				.Include(p => p.Products)
					.ThenInclude(pr => pr.Brand)
				.Include(p => p.Products)
					.ThenInclude(pr => pr.Category)
				.Include(p => p.SmallCollectionPoints);

			if (!string.IsNullOrEmpty(recyclerId))
			{
				query = query.Where(p => p.SmallCollectionPoints.RecyclingCompanyId == recyclerId);
			}

			if (!string.IsNullOrEmpty(status))
			{
				var trimmedStatus = status.Trim().ToLower();
				query = query.Where(p => !string.IsNullOrEmpty(p.Status) && p.Status.ToLower() == trimmedStatus);
			}

			var totalCount = await query.CountAsync();

			var pagedPackages = await query
				.OrderByDescending(p => p.CreateAt)
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (pagedPackages, totalCount);
		}
	}

}

