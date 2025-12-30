using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IPostService
	{
		Task<bool> AddPost(CreatePostModel createPostRequest);

		Task<List<PostSummaryModel>> GetAll();
		Task<List<PostDetailModel>> GetPostBySenderId(Guid senderId);
		Task<PostDetailModel> GetById(Guid id);
		Task<bool> ApprovePost(List<Guid> postId);

		Task<bool> RejectPost(List<Guid> postId, string rejectMessage);
		Task<PagedResultModel<PostSummaryModel>> GetPagedPostsAsync(PostSearchQueryModel model);
	}
}
