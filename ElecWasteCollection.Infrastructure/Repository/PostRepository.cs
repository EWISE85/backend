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
	public class PostRepository : GenericRepository<Post>, IPostRepository
	{
		public PostRepository(DbContext context) : base(context)
		{
		}

		public async Task<List<Post>> GetAllPostsWithDetailsAsync()
		{
			return await _dbSet
				.Include(p => p.Sender)
				.Include(p => p.Images)
				.Include(p => p.Product)
					.ThenInclude(pr => pr.Category)
						.ThenInclude(c => c.ParentCategory)
				//.Include(p => p.Product)
				//	.ThenInclude(pr => pr.Images)
				.AsNoTracking()
				.ToListAsync();
		}

		//public async Task<(List<Post> Items, int TotalCount)> GetPagedPostsAsync(string? status,string? search,string? order,int page,int limit)
		//{
		//	var query = _dbSet.AsNoTracking()
		//		.Include(p => p.Sender)
		//		.Include(p => p.Product).ThenInclude(pr => pr.Category).ThenInclude(c => c.ParentCategory)
		//		.Include(p => p.Product).ThenInclude(pr => pr.ProductImages)
		//		.Include(p => p.Product).ThenInclude(pr => pr.Brand)
		//		.AsQueryable();

		//	if (!string.IsNullOrEmpty(status))
		//	{
		//		var trimmedStatus = status.Trim().ToLower();
		//		query = query.Where(p => !string.IsNullOrEmpty(p.Status) && p.Status.ToLower() == trimmedStatus);
		//	}

		//	if (!string.IsNullOrEmpty(search))
		//	{
		//		string searchLower = search.ToLower();
		//		query = query.Where(p =>
		//			p.Product.Category.Name.ToLower().Contains(searchLower));
		//	}

		//	if (status == "Chờ Duyệt")
		//	{
		//		query = query.OrderBy(p => p.Date);
		//	}
		//	else if (status == "Đã Duyệt" || status == "Đã Từ Chối")
		//	{
		//		query = query.OrderByDescending(p => p.Date);
		//	}
		//	else
		//	{
		//		if (order?.ToUpper() == "ASC")
		//			query = query.OrderBy(p => p.Date);
		//		else
		//			query = query.OrderByDescending(p => p.Date);
		//	}

		//	int totalCount = await query.CountAsync();

		//	var items = await query
		//		.Skip((page - 1) * limit)
		//		.Take(limit)
		//		.ToListAsync();

		//	return (items, totalCount);
		//}
		public async Task<(List<Post> Items, int TotalCount)> GetPagedPostsAsync(string? status, string? search, string? order, int page, int limit, DateOnly? startTime, DateOnly? endTime)
		{
			var query = _dbSet.AsNoTracking()
				.Include(p => p.Sender)
				.Include(p => p.Images)
				.Include(p => p.Product).ThenInclude(pr => pr.Category).ThenInclude(c => c.ParentCategory)
				.Include(p => p.Product).ThenInclude(pr => pr.Brand)
				.AsQueryable();

			// 1. Filter theo status
			if (!string.IsNullOrEmpty(status))
			{
				var trimmedStatus = status.Trim().ToLower();
				query = query.Where(p => !string.IsNullOrEmpty(p.Status) && p.Status.ToLower() == trimmedStatus);
			}

			// 2. Filter theo search term
			if (!string.IsNullOrEmpty(search))
			{
				string searchLower = search.ToLower();

				query = query.Where(p =>
					(p.Product != null && p.Product.Category != null && p.Product.Category.Name.ToLower().Contains(searchLower)) ||
					(p.Description != null && p.Description.ToLower().Contains(searchLower)) ||
					(p.Address != null && p.Address.ToLower().Contains(searchLower)));
			}

			if (startTime.HasValue)
			{
				// Chuyển DateOnly thành DateTime ở thời điểm 00:00:00 và set Kind là UTC
				var startDateTime = DateTime.SpecifyKind(
					startTime.Value.ToDateTime(TimeOnly.MinValue),
					DateTimeKind.Utc
				);
				query = query.Where(p => p.Date >= startDateTime);
			}

			if (endTime.HasValue)
			{
				// Lấy < mốc 00:00:00 của ngày hôm sau và set Kind là UTC
				var endDateTime = DateTime.SpecifyKind(
					endTime.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
					DateTimeKind.Utc
				);
				query = query.Where(p => p.Date < endDateTime);
			}

			// 4. Xử lý OrderBy
			var choDuyetStatus = PostStatus.CHO_DUYET.ToString().ToLower();
			var daDuyetStatus = PostStatus.DA_DUYET.ToString().ToLower();
			var tuChoiStatus = PostStatus.DA_TU_CHOI.ToString().ToLower();

			var currentStatus = status?.Trim().ToLower();

			if (currentStatus == choDuyetStatus)
			{
				query = query.OrderBy(p => p.Date);
			}
			else if (currentStatus == daDuyetStatus || currentStatus == tuChoiStatus)
			{
				query = query.OrderByDescending(p => p.Date);
			}
			else
			{
				if (order?.ToUpper() == "ASC")
					query = query.OrderBy(p => p.Date);
				else
					query = query.OrderByDescending(p => p.Date);
			}

			// 5. Execute Count và Paging
			int totalCount = await query.CountAsync();

			var items = await query
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<List<Post>> GetPostsBySenderIdWithDetailsAsync(Guid senderId)
		{
			return await _dbSet
				.Where(p => p.SenderId == senderId)
				.Include(p => p.Sender)
				.Include(p => p.Images)
				.Include(p => p.Product).ThenInclude(pr => pr.Brand)
				.Include(p => p.Product).ThenInclude(pr => pr.Category).ThenInclude(c => c.ParentCategory)
				//.Include(p => p.Product).ThenInclude(pr => pr.Images)
				.Include(p => p.Product).ThenInclude(pr => pr.ProductValues)
				.AsNoTracking()
				.ToListAsync();
		}

		public IQueryable<Post> GetPostsQuery()
		{
			return _dbSet
				.Include(p => p.Sender)
				.Include(p => p.Product).ThenInclude(pr => pr.Category).ThenInclude(c => c.ParentCategory)
				.Include(p => p.Product).ThenInclude(pr => pr.Images)
				.AsNoTracking();
		}

		public async Task<Post?> GetPostWithDetailsAsync(Guid id)
		{
			return await _dbSet
				.Where(p => p.PostId == id)
				.Include(p => p.Sender).ThenInclude(s => s.Role)
				.Include(p => p.Images)
				.Include(p => p.Product).ThenInclude(pr => pr.Brand)
				.Include(p => p.Product).ThenInclude(pr => pr.Category).ThenInclude(c => c.ParentCategory)
				//.Include(p => p.Product).ThenInclude(pr => pr.Images)
				.Include(p => p.Product).ThenInclude(pr => pr.ProductValues)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}
	}
}
