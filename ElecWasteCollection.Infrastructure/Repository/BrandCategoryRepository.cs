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
	public class BrandCategoryRepository : GenericRepository<BrandCategory>, IBrandCategoryRepository
	{
		public BrandCategoryRepository(DbContext context) : base(context)
		{
		}
		public Task<(List<BrandCategory> Items, int TotalCount)> GetPagedBrandForAdmin(Guid categoryId, string? brandName ,string? status, int page, int limit)
		{
			var query = _dbSet.AsNoTracking()
				.Include(bc => bc.Brand)
				.Include(bc => bc.Category)
				.AsQueryable();
			query = query.Where(b => b.CategoryId == categoryId);
			if (!string.IsNullOrEmpty(status))
			{
				query = query.Where(b => b.Status == status);
			}
			if (!string.IsNullOrEmpty(brandName))
			{
				query = query.Where(b => b.Brand.Name.Contains(brandName));
			}
			var totalCount = query.Count();

			var items = query
				.OrderBy(bc => bc.Brand.Name)
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToList();

			return Task.FromResult((items, totalCount));

		}
	}
}
