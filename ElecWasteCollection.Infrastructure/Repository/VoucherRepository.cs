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
	public class VoucherRepository : GenericRepository<Voucher>, IVoucherRepository
	{
		public VoucherRepository(DbContext context) : base(context)
		{
		}

		public async Task<(List<Voucher> Items, int TotalCount)> GetPagedVoucher(string? name, string? status, int page, int limit)
		{
			var query = _dbSet.AsNoTracking();
			if (!string.IsNullOrEmpty(name))
			{
				query = query.Where(v => v.Name.Contains(name));
			}
			if (!string.IsNullOrEmpty(status)) {
				query = query.Where(v => v.Status == status);
			}
			var totalCount = await query.CountAsync();

			var items = await query
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<(List<Voucher> Items, int TotalCount)> GetPagedVoucherByUser(Guid userId, string? name, string? status, int page, int limit)
		{
			var query = _dbSet.AsNoTracking()
				.Include(v => v.UserVouchers)
				.AsQueryable();

			query = query.Where(v => v.UserVouchers.Any(uv => uv.UserId == userId));

			if (!string.IsNullOrEmpty(name))
			{
				query = query.Where(v => v.Name.Contains(name));
			}
			if (!string.IsNullOrEmpty(status))
			{
				query = query.Where(v => v.Status == status);
			}
			int totalCount = await query.CountAsync();

			var items = await query
						.Skip((page - 1) * limit)
						.Take(limit)
						.ToListAsync();

			return (items, totalCount);
		}
	}
}
