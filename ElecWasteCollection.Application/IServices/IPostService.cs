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
		List<PostModel> GetAll();
		List<PostModel> GetPostBySenderId(Guid senderId);
		PostModel GetById(Guid id);
		bool ApprovePost(Guid postId);

		bool RejectPost(Guid postId, string rejectMessage);
	}
}
