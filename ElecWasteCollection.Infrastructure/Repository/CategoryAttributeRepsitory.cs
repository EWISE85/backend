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
	public class CategoryAttributeRepsitory : GenericRepository<CategoryAttributes>, ICategoryAttributeRepsitory
	{
		public CategoryAttributeRepsitory(DbContext context) : base(context)
		{
		}

		public async Task<List<CategoryAttributes>> GetCategoryAttributeForAdmin(Guid categoryId, string? status)
		{
			var query = _dbSet.AsNoTracking().Include(ca => ca.Attribute)
				.Where(ca => ca.CategoryId == categoryId);
			if (!string.IsNullOrEmpty(status))
			{
				query = query.Where(ca => ca.Status == status);
			}
			return await query.ToListAsync();
		}
	}
}
