using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface IPostRepository : IGenericRepository<Post>
	{
		Task<List<Post>> GetAllPostsWithDetailsAsync();
		Task<Post?> GetPostWithDetailsAsync(Guid id);
		Task<List<Post>> GetPostsBySenderIdWithDetailsAsync(Guid senderId);

		Task<(List<Post> Items, int TotalCount)> GetPagedPostsAsync(string? status,string? search,string? order,int page,int limit);
	}
}
