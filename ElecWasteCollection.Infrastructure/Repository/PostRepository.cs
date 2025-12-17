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
				.Include(p => p.Product)
					.ThenInclude(pr => pr.Category)
						.ThenInclude(c => c.ParentCategory)
				.Include(p => p.Product)
					.ThenInclude(pr => pr.ProductImages)
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<(List<Post> Items, int TotalCount)> GetPagedPostsAsync(string? status,string? search,string? order,int page,int limit)
		{
			var query = _dbSet.AsNoTracking()
				.Include(p => p.Sender)
				.Include(p => p.Product).ThenInclude(pr => pr.Category).ThenInclude(c => c.ParentCategory)
				.Include(p => p.Product).ThenInclude(pr => pr.ProductImages)
				.AsQueryable();

			if (!string.IsNullOrEmpty(status))
			{
				query = query.Where(p => p.Status == status);
			}

			if (!string.IsNullOrEmpty(search))
			{
				string searchLower = search.ToLower();
				query = query.Where(p =>
					p.Product.Category.Name.ToLower().Contains(searchLower));
			}

			if (status == "Chờ Duyệt")
			{
				query = query.OrderBy(p => p.Date);
			}
			else if (status == "Đã Duyệt" || status == "Đã Từ Chối")
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
				.Include(p => p.Product).ThenInclude(pr => pr.Brand)
				.Include(p => p.Product).ThenInclude(pr => pr.Category).ThenInclude(c => c.ParentCategory)
				.Include(p => p.Product).ThenInclude(pr => pr.ProductImages)
				.Include(p => p.Product).ThenInclude(pr => pr.ProductValues)
				.AsNoTracking()
				.ToListAsync();
		}

		public IQueryable<Post> GetPostsQuery()
		{
			return _dbSet
				.Include(p => p.Sender)
				.Include(p => p.Product).ThenInclude(pr => pr.Category).ThenInclude(c => c.ParentCategory)
				.Include(p => p.Product).ThenInclude(pr => pr.ProductImages)
				.AsNoTracking();
		}

		public async Task<Post?> GetPostWithDetailsAsync(Guid id)
		{
			return await _dbSet
				.Where(p => p.PostId == id)
				.Include(p => p.Sender)
				.Include(p => p.Product).ThenInclude(pr => pr.Brand)
				.Include(p => p.Product).ThenInclude(pr => pr.Category).ThenInclude(c => c.ParentCategory)
				.Include(p => p.Product).ThenInclude(pr => pr.ProductImages)
				.Include(p => p.Product).ThenInclude(pr => pr.ProductValues)
				.AsNoTracking()
				.FirstOrDefaultAsync();
		}
	}
}
