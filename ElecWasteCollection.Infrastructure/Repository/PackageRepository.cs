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

		public async Task<(List<Packages> Items, int TotalCount)> GetPagedPackagesWithDetailsByCompanyAsync(
	string? companyId,
	DateTime? startDate,
	DateTime? endDate,
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

			if (startDate.HasValue && startDate.Value.Kind == DateTimeKind.Unspecified)
			{
				startDate = DateTime.SpecifyKind(startDate.Value, DateTimeKind.Utc);
			}

			if (endDate.HasValue && endDate.Value.Kind == DateTimeKind.Unspecified)
			{
				endDate = DateTime.SpecifyKind(endDate.Value, DateTimeKind.Utc);
			}

			

			if (!string.IsNullOrEmpty(companyId))
			{
				query = query.Where(p => p.SmallCollectionPoints.CompanyId == companyId);
			}

			if (startDate.HasValue)
			{
				query = query.Where(p => p.CreateAt >= startDate.Value);
			}

			if (endDate.HasValue)
			{
				query = query.Where(p => p.CreateAt <= endDate.Value);
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

		public async Task<(List<Packages> Items, int TotalCount)> GetPagedPackagesByDeliveryQrCodeAsync(
	string deliveryQrCode,
	int page,
	int limit)
		{
			var query = _dbSet.AsNoTracking().AsSplitQuery();

			query = query
				.Include(p => p.Products)
				.Include(p => p.PackageStatusHistories)
				.Include(p => p.SmallCollectionPoints)
					.ThenInclude(scp => scp.RecyclingCompany); 

			if (!string.IsNullOrEmpty(deliveryQrCode))
			{
				query = query.Where(p => p.DeliveryQrCode == deliveryQrCode);
			}

			var totalCount = await query.CountAsync();

			var pagedPackages = await query
				.OrderByDescending(p => p.CreateAt) 
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (pagedPackages, totalCount);
		}

		public async Task<(List<Packages> Items, int TotalCount)> GetTrackingPackage(string? recyclerId, DateOnly? fromDate, DateOnly? toDate, string? smallCollectionPointId,
 string? packageId, string? status, int page, int limit)
		{
			var query = _dbSet.AsNoTracking().AsSplitQuery();

			query = query
				.Include(p => p.Products)
				.Include(p => p.PackageStatusHistories)
				.Include(p => p.SmallCollectionPoints)
					.ThenInclude(scp => scp.RecyclingCompany);
			if (!string.IsNullOrEmpty(recyclerId))
			{
				query = query.Where(p => p.SmallCollectionPoints.RecyclingCompanyId == recyclerId);
			}
			if (!string.IsNullOrEmpty(smallCollectionPointId))
			{
				query = query.Where(p => p.SmallCollectionPointsId == smallCollectionPointId);
			}
			if (!string.IsNullOrEmpty(packageId))
			{
				query = query.Where(p => p.PackageId == packageId);
			}
			if (!string.IsNullOrEmpty(status))
			{
				var trimmedStatus = status.Trim().ToLower();
				query = query.Where(p => !string.IsNullOrEmpty(p.Status) && p.Status.ToLower() == trimmedStatus);
			}
			if (fromDate.HasValue)
			{
				var fromDateTime = DateTime.SpecifyKind(fromDate.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
				query = query.Where(p => p.DeliveryHandoverAt >= fromDateTime);
			}
			if (toDate.HasValue)
			{
				var toDateTime = DateTime.SpecifyKind(toDate.Value.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
				query = query.Where(p => p.DeliveryHandoverAt <= toDateTime);
			}
			var totalCount = await query.CountAsync();

			var pagedPackages = await query
				.OrderByDescending(p => p.DeliveryHandoverAt)
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (pagedPackages, totalCount);
		}
	}

}

