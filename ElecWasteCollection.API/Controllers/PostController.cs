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
				//Name = newItem.Name,
				CollectionSchedule = newItem.CollectionSchedule,
				SenderId = newItem.SenderId,
				Product = new CreateProductModel
				{
					ParentCategoryId = newItem.Product.ParentCategoryId,
					SubCategoryId = newItem.Product.SubCategoryId,
					BrandId = newItem.Product.BrandId,
					Attributes = newItem.Product.Attributes?.Select(attr => new ProductValueModel
					{
						AttributeId = attr.AttributeId,
						OptionId = attr.OptionId,
						Value = attr.Value
					}).ToList()
				}
			};
			var result = await _postService.AddPost(model);
			if (!result)
			{
				return StatusCode(400, "An error occurred while creating the post.");
			}


			return Ok(new { message = "Post created successfully.", item = result });
		}
		[HttpGet]
		public async Task<IActionResult> GetAllPosts()
		{
			var posts = await _postService.GetAll();
			return Ok(posts);
		}
		[HttpGet("{postId}")]
		public async Task<IActionResult> GetPostById(Guid postId)
		{
			var post = await _postService.GetById(postId);
			if (post == null)
			{
				return NotFound($"Post with ID {postId} not found.");
			}
			return Ok(post);
		}
		[HttpGet("sender/{senderId}")]
		public async Task<IActionResult> GetPostsBySenderId([FromRoute] Guid senderId)
		{
			var posts = await _postService.GetPostBySenderId(senderId);
			return Ok(posts);
		}

		[HttpPut("approve")]
		public async Task<IActionResult> ApprovePost([FromBody] ApprovePostRequest request)
		{
			var isApproved = await _postService.ApprovePost(request.PostIds);

			if (isApproved)
			{
				return Ok(new { message = $"Post approved successfully." });
			}
			else
			{
				return StatusCode(400, $"An error occurred while approving the post.");
			}
		}
		[HttpPut("reject")]
		public async Task<IActionResult> RejectPost([FromBody] RejectPostRequest rejectPostRequest)
		{
			var isRejected = await _postService.RejectPost(rejectPostRequest.PostIds, rejectPostRequest.RejectMessage);
			if (isRejected)
			{
				return Ok(new { message = $"Post  rejected successfully." });
			}
			else
			{
				return StatusCode(400, $"An error occurred while rejecting the post.");
			}
		}

		[HttpGet("filter")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<IActionResult> GetPagedPosts(
			[FromQuery] PostSearchQueryModel parameters)
		{
			// Gọi thẳng đến service, service sẽ lo toàn bộ logic
			var pagedResult = await _postService.GetPagedPostsAsync(parameters);
			return Ok(pagedResult);
		}

	}
}
