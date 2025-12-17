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
	public class CollectionCompanyRepository : GenericRepository<CollectionCompany>, ICollectionCompanyRepository
	{
		public CollectionCompanyRepository(DbContext context) : base(context)
		{
		}

		public async Task<(List<CollectionCompany> Items, int TotalCount)> GetPagedCompaniesAsync(string? status,int page,int limit)
		{
			var query = _dbSet.AsNoTracking();

			if (!string.IsNullOrEmpty(status))
			{
				query = query.Where(c => c.Status == status);
			}

			var totalCount = await query.CountAsync();

			// 3. Phân trang & Lấy dữ liệu
			var items = await query
				.OrderByDescending(c => c.Created_At)
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (items, totalCount);
		}
	}
}
