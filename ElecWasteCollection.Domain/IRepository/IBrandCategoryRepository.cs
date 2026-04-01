using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface IBrandCategoryRepository : IGenericRepository<BrandCategory>	
	{
		Task<(List<BrandCategory> Items, int TotalCount)> GetPagedBrandForAdmin(Guid categoryId, string? brandName ,string? status, int page, int limit);
	}
}
