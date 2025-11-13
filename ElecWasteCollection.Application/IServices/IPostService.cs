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
		Task<PostDetailModel> AddPost(CreatePostModel createPostRequest);
		PostDetailModel AddPostByAdminWarehouse(CreateProductAtWarehouseModel createPostRequest);

		List<PostSummaryModel> GetAll();
		List<PostDetailModel> GetPostBySenderId(Guid senderId);
		PostDetailModel GetById(Guid id);
		Task<bool> ApprovePost(Guid postId);

		Task<bool> RejectPost(Guid postId, string rejectMessage);
		Task<PagedResultModel<PostSummaryModel>> GetPagedPostsAsync(PostSearchQueryModel model);
	}
}
