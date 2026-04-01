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
	public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
	{
		public CategoryRepository(DbContext context) : base(context)
		{
		}

		public Task<(List<Category> Items, int TotalCount)> GetPagedCategoryForAdmin(Guid parentId, string? name ,string? status, int page, int limit)
		{
			var query = _dbSet.Where(c => c.ParentCategoryId == parentId);

			if (!string.IsNullOrEmpty(status))
			{
				query = query.Where(c => c.Status == status);
			}
			if (!string.IsNullOrEmpty(name))
			{
				query = query.Where(c => c.Name.Contains(name));
			}

			var totalCount = query.Count();

			var items = query
				.OrderBy(c => c.Name)
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToList();

			return Task.FromResult((items, totalCount));
		}
	}
}
