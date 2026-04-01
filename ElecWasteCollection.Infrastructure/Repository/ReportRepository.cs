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
	public class ReportRepository : GenericRepository<UserReport>, IReportRepository
	{
		public ReportRepository(DbContext context) : base(context)
		{
		}

		public async Task<(List<UserReport> Items, int TotalCount)> GetPagedReport(string? type, string? status, DateOnly? start, DateOnly? end, int page, int limit)
		{
			var query = _dbSet.AsNoTracking()
				.Include(r => r.User)
				.AsQueryable();
			if (!string.IsNullOrEmpty(type))
			{
				query = query.Where(r => r.ReportType == type);
			}
			if (!string.IsNullOrEmpty(status))
			{
				query = query.Where(r => r.Status == status);
			}
			if (start.HasValue)
			{
				var startUtc = DateTime.SpecifyKind(start.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
				query = query.Where(r => r.CreatedAt >= startUtc);
			}

			if (end.HasValue)
			{
				var endUtc = DateTime.SpecifyKind(end.Value.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
				query = query.Where(r => r.CreatedAt <= endUtc);
			}
			var totalCount = await query.CountAsync();

			var items = await query
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<(List<UserReport> Items, int TotalCount)> GetPagedReportForUser(Guid? userId, string? type, string? status, DateOnly? start, DateOnly? end, int page, int limit)
		{
			var query = _dbSet.AsNoTracking()
				.Include(r => r.User)
				.AsQueryable();
			if (userId.HasValue)
			{
				query = query.Where(r => r.UserId == userId.Value);
			}
			if (!string.IsNullOrEmpty(type))
			{
				query = query.Where(r => r.ReportType == type);
			}
			if (!string.IsNullOrEmpty(status))
			{
				query = query.Where(r => r.Status == status);
			}
			if (start.HasValue)
			{
				var startUtc = DateTime.SpecifyKind(start.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
				query = query.Where(r => r.CreatedAt >= startUtc);
			}

			if (end.HasValue)
			{
				var endUtc = DateTime.SpecifyKind(end.Value.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
				query = query.Where(r => r.CreatedAt <= endUtc);
			}
			var totalCount = await query.CountAsync();

			var items = await query
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (items, totalCount);
		}
	}
}
