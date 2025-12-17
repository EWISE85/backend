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
	public class PointTransactionRepository : GenericRepository<PointTransactions>, IPointTransactionRepository
	{
		public PointTransactionRepository(DbContext context) : base(context)
		{
		}
		public async Task<List<PointTransactions>> GetPointHistoryWithProductImagesAsync(Guid userId)
		{
			return await _dbSet.AsNoTracking()
				.AsSplitQuery() 

				.Include(pt => pt.Product) 
					.ThenInclude(p => p.ProductImages) 

				.Where(pt => pt.UserId == userId)

				.OrderByDescending(pt => pt.CreatedAt)
				.ToListAsync();
		}
	}
}
