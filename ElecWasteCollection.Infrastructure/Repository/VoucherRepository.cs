using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.Repository
{
	public class VoucherRepository : GenericRepository<Voucher>, IVoucherRepository
	{
		public VoucherRepository(DbContext context) : base(context)
		{
		}

		// Dành cho Admin: Xem tất cả Voucher
		public async Task<(List<Voucher> Items, int TotalCount)> GetPagedVoucher(string? name, string? status, int page, int limit)
		{
			var query = _dbSet.AsNoTracking();

			if (!string.IsNullOrWhiteSpace(name))
			{
				query = query.Where(v => v.Name.Contains(name));
			}
			if (!string.IsNullOrWhiteSpace(status))
			{
				query = query.Where(v => v.Status == status);
			}

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(v => v.VoucherId)
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<(List<Voucher> Items, int TotalCount)> GetPagedVoucherByUser(Guid userId, string? name, string? status, int page, int limit)
		{
			var query = _dbSet.AsNoTracking()
				.Include(v => v.UserVouchers.Where(uv => uv.UserId == userId))
				.AsQueryable();

			query = query.Where(v => v.UserVouchers.Any(uv => uv.UserId == userId));

			if (!string.IsNullOrWhiteSpace(name))
			{
				query = query.Where(v => v.Name.Contains(name));
			}

			if (!string.IsNullOrWhiteSpace(status))
			{
				query = query.Where(v => v.Status == status);
			}

			int totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(v => v.UserVouchers.First().ReceivedAt) // Chuẩn
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<(List<Voucher> Items, int TotalCount)> GetPagedVoucherForUser(
	Guid userId,
	string? name,
	string? status,
	int page,
	int limit)
		{
			var query = _dbSet.AsNoTracking();

			if (!string.IsNullOrWhiteSpace(name))
			{
				query = query.Where(v => v.Name.Contains(name));
			}

			if (!string.IsNullOrWhiteSpace(status))
			{
				query = query.Where(v => v.Status == status);
			}

			query = query.Where(v => v.Quantity > 0);

			query = query.Where(v => !v.UserVouchers.Any(uv => uv.UserId == userId));

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(v => v.StartAt)
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (items, totalCount);
		}
	}
}