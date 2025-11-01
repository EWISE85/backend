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
				Description = newItem.Description,
				Images = newItem.Images,
				Name = newItem.Name,
				CollectionSchedule = newItem.CollectionSchedule,
				SenderId = newItem.SenderId,
				Product = new CreateProductModel
				{
					ParentCategoryId = newItem.Product.ParentCategoryId,
					SubCategoryId = newItem.Product.SubCategoryId,
					SizeTierId = newItem.Product.SizeTierId,
					Attributes = newItem.Product.Attributes?.Select(attr => new ProductValueModel
					{
						AttributeId = attr.AttributeId,
						Value = attr.Value
					}).ToList()
				}
			};
			var result =  await _postService.AddPost(model);
			if (result == null)
			{
				return StatusCode(400, "An error occurred while creating the post.");
			}


			return Ok(new { message = "Post created successfully.", item = result });
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
		public async Task<IActionResult> ApprovePost(Guid postId)
		{
			var isApproved = await _postService.ApprovePost(postId);

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
