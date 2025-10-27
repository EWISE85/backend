using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/posts/")]
	[ApiController]
	public class PostController : ControllerBase
	{
		private readonly IPostService _postService;
		public PostController(IPostService postService)
		{
			_postService = postService;
		}
		[HttpPost()]
		public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest newItem)
		{
			if (newItem == null)
			{
				return BadRequest("Invalid data.");
			}

			var model = new CreatePostModel
			{
				Address = newItem.Address,
				Category = newItem.Category,
				Description = newItem.Description,
				Images = newItem.Images,
				Name = newItem.Name,
				CollectionSchedule = newItem.CollectionSchedule,
				SenderId = newItem.SenderId
			};
			var result =  await _postService.AddPost(model);
			if (!result)
			{
				return StatusCode(400, "An error occurred while creating the post.");
			}


			return Ok(new { message = "Post created successfully.", item = newItem });
		}
		[HttpGet]
		public IActionResult GetAllPosts()
		{
			var posts = _postService.GetAll();
			return Ok(posts);
		}
		[HttpGet("{postId}")]
		public IActionResult GetPostById(Guid postId)
		{
			var post = _postService.GetById(postId);
			if (post == null)
			{
				return NotFound($"Post with ID {postId} not found.");
			}
			return Ok(post);
		}
		[HttpGet("sender/{senderId}")]
		public IActionResult GetPostsBySenderId([FromRoute] Guid senderId)
		{
			var posts = _postService.GetPostBySenderId(senderId);
			return Ok(posts);
		}

		[HttpPut("approve/{postId}")]
		public IActionResult ApprovePost(Guid postId)
		{
			var isApproved = _postService.ApprovePost(postId);

			if (isApproved)
			{
				return Ok(new { message = $"Post {postId} approved successfully." });
			}
			else
			{
				return StatusCode(400, $"An error occurred while approving the post {postId}.");
			}
		}
		[HttpPut("reject/{postId}")]
		public IActionResult RejectPost([FromRoute] Guid postId, [FromBody] RejectPostRequest rejectPostRequest)
		{
			var isRejected = _postService.RejectPost(postId, rejectPostRequest.RejectMessage);
			if (isRejected)
			{
				return Ok(new { message = $"Post {postId} rejected successfully." });
			}
			else
			{
				return StatusCode(400, $"An error occurred while rejecting the post {postId}.");
			}
		}



	}
}
