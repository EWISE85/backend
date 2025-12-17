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
	public class ProductRepository : GenericRepository<Products>, IProductRepository
	{
		public ProductRepository(DbContext context) : base(context)
		{
		}
		public async Task<Products?> GetProductByQrCodeWithDetailsAsync(string qrcode)
		{
			var query = _dbSet.AsNoTracking()
				.AsSplitQuery();

			query = query
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.Include(p => p.ProductImages)
				.Include(p => p.PointTransactions)
				.Include(p => p.Posts);

			return await query.FirstOrDefaultAsync(p => p.QRCode == qrcode);
		}
		public async Task<List<Products>> GetProductsByPackageIdWithDetailsAsync(string packageId)
		{
			var query = _dbSet.AsNoTracking()
				.AsSplitQuery();

			query = query
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.Include(p => p.ProductValues)
					.ThenInclude(pv => pv.Attribute);

			query = query.Where(p => p.PackageId == packageId);

			return await query.ToListAsync();
		}
		public async Task<List<Products>> GetProductsCollectedByRouteAsync(DateOnly fromDate, DateOnly toDate, string smallCollectionPointId)
		{
			var query = _dbSet.AsNoTracking()
				.AsSplitQuery()
				.Include(p => p.CollectionRoutes).ThenInclude(r => r.CollectionGroup).ThenInclude(g => g.Shifts).ThenInclude(s => s.Vehicle)
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.Include(p => p.ProductImages)
				.Include(p => p.PointTransactions)
				.Include(p => p.Posts) 
				.AsQueryable();

			query = query.Where(p =>
				p.CollectionRoutes.Any(r =>
					r.CollectionDate >= fromDate &&
					r.CollectionDate <= toDate &&
					r.CollectionGroup != null &&
					r.CollectionGroup.Shifts != null &&
					r.CollectionGroup.Shifts.Vehicle != null &&
					r.CollectionGroup.Shifts.Vehicle.Small_Collection_Point == smallCollectionPointId
				)
			);

			return await query.ToListAsync();
		}

		public async Task<List<Products>> GetDirectlyEnteredProductsAsync(DateOnly fromDate, DateOnly toDate, string smallCollectionPointId)
		{
			var query = _dbSet.AsNoTracking()
				.AsSplitQuery()
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.Include(p => p.ProductImages)
				.Include(p => p.PointTransactions)
				.Include(p => p.Posts)
				.AsQueryable();

			query = query.Where(p =>
				p.SmallCollectionPointId == smallCollectionPointId &&
				p.CreateAt >= fromDate &&
				p.CreateAt <= toDate &&
				p.PackageId == null &&
				p.Status == "Nhập kho"
			);

			return await query.ToListAsync();
		}
		public async Task<Products?> GetProductWithDetailsAsync(Guid productId)
		{
			var query = _dbSet.AsNoTracking()
				.AsSplitQuery(); 

			query = query
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.Include(p => p.ProductImages)
				.Include(p => p.Posts);

			query = query.Where(p => p.ProductId == productId);

			return await query.FirstOrDefaultAsync();
		}
		public async Task<List<Products>> GetProductsBySenderIdWithDetailsAsync(Guid senderId)
		{
			var query = _dbSet.AsNoTracking()
				.AsSplitQuery();

			query = query
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.Include(p => p.ProductImages)
				.Include(p => p.PointTransactions)
				.Include(p => p.Posts);

			query = query.Where(p =>
				p.Posts.Any(post => post.SenderId == senderId)
			);

			return await query
				.OrderByDescending(p => p.CreateAt) 
				.ToListAsync();
		}
		public async Task<Products?> GetProductDetailWithAllRelationsAsync(Guid productId)
		{
			var query = _dbSet.AsNoTracking()
				.AsSplitQuery(); 

			query = query
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.Include(p => p.ProductImages)
				.Include(p => p.PointTransactions)

				.Include(p => p.Posts).ThenInclude(pst => pst.Sender) 
				.Include(p => p.ProductValues).ThenInclude(pv => pv.Attribute) 

			
				.Include(p => p.CollectionRoutes.OrderByDescending(r => r.CollectionDate).Take(1))
					.ThenInclude(r => r.CollectionGroup)
						.ThenInclude(g => g.Shifts)
							.ThenInclude(s => s.Collector);

			return await query.FirstOrDefaultAsync(p => p.ProductId == productId);
		}

		public async Task<(List<Products> Items, int TotalCount)> GetPagedProductsForAdminAsync(
			int page,
			int limit,
			DateOnly? fromDate,
			DateOnly? toDate,
			string? categoryName,
			string? collectionCompanyId)
		{
			var query = _dbSet.AsNoTracking().AsSplitQuery();

			query = query
				.Include(p => p.Category)
				.Include(p => p.Brand)
				.Include(p => p.ProductImages)
				.Include(p => p.PointTransactions)
				.Include(p => p.Posts).ThenInclude(pst => pst.Sender).ThenInclude(s => s.UserAddresses) 
				.Include(p => p.CollectionRoutes).ThenInclude(r => r.CollectionGroup)
					.ThenInclude(g => g.Shifts)
						.ThenInclude(s => s.Collector);			

			if (collectionCompanyId != null)
			{
				var relevantScpIds = _context.Set<SmallCollectionPoints>()
											 .Where(scp => scp.CompanyId == collectionCompanyId)
											 .Select(scp => scp.SmallCollectionPointsId);
				query = query.Where(p => p.CollectionRoutes.Any(route =>
					route.CollectionGroup.Shifts.Collector.SmallCollectionPointId != null &&
					relevantScpIds.Contains(route.CollectionGroup.Shifts.Collector.SmallCollectionPointId)
				));
			}

			if (!string.IsNullOrEmpty(categoryName))
			{
				query = query.Where(p => p.Category.Name.Contains(categoryName));
			}

			if (fromDate.HasValue)
			{
				query = query.Where(p => p.CreateAt.HasValue && p.CreateAt.Value >= fromDate.Value);
			}
			if (toDate.HasValue)
			{
				query = query.Where(p => p.CreateAt.HasValue && p.CreateAt.Value <= toDate.Value);
			}

			var totalRecords = await query.CountAsync();

			var productsPaged = await query
				.OrderByDescending(p => p.CreateAt)
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (productsPaged, totalRecords);
		}
	}
}

