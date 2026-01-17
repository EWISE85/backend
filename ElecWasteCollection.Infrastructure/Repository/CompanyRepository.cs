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
	public class CompanyRepository : GenericRepository<Company>, ICompanyRepository
	{
		public CompanyRepository(DbContext context) : base(context)
		{
		}

		public async Task<(List<Company> Items, int TotalCount)> GetPagedCompaniesAsync(string? status,int page,int limit)
		{
			var query = _dbSet.AsNoTracking();

			if (!string.IsNullOrEmpty(status))
			{
				var trimmedStatus = status.Trim().ToLower();
				query = query.Where(c => !string.IsNullOrEmpty(c.Status) && c.Status.ToLower() == trimmedStatus);
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

        public async Task<(List<Company> Items, int TotalCount)> GetPagedCollectionCompaniesAsync( int page, int limit)
        {
            var query = _dbSet
                .AsNoTracking()
                .Where(c =>
                    c.CompanyType ==
                    CompanyType.CTY_THU_GOM.ToString());

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.Created_At)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
