using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ElecWasteCollection.Application.Services
{
	public class PostService : IPostService
	{
		private static List<Post> posts = new List<Post>();
		private const string ImaggaApiKey = "acc_b80eaae365fbf2f";
		private const string ImaggaApiSecret = "ac0c2b3adc747be522c11368f95882b3";
		private const double ConfidenceThreshold = 80.0;
		private static readonly HttpClient _httpClient = new HttpClient();
		private readonly IUserService _userService;
		public PostService(IUserService userService)
		{
			_userService = userService;
		}

		public async Task<bool> AddPost(CreatePostModel createPostRequest)
		{
			string postStatus = "Chờ Duyệt"; // Mặc định là Chờ Duyệt

			try
			{
				if (createPostRequest.Images != null && createPostRequest.Images.Any())
				{
					var checkTasks = createPostRequest.Images
						.Select(imageUrl => CheckImageCategoryAsync(imageUrl, createPostRequest.Category))
						.ToList();

					// Chờ tất cả các Task hoàn thành
					var results = await Task.WhenAll(checkTasks);

					// Kiểm tra kết quả: chỉ "Đã Duyệt" nếu TẤT CẢ đều trả về true
					var allImagesMatch = results.All(isMatch => isMatch);

					if (allImagesMatch)
					{
						postStatus = "Đã Duyệt";
					}
				}

				var newPost = new Post
				{
					Id = Guid.NewGuid(),
					SenderId = createPostRequest.SenderId,
					Name = createPostRequest.Name,
					Category = createPostRequest.Category,
					Description = createPostRequest.Description,
					Date = DateTime.Now,
					Address = createPostRequest.Address,
					Images = createPostRequest.Images,
					ScheduleJson = JsonSerializer.Serialize(createPostRequest.CollectionSchedule),
					Status = postStatus
				};
				Console.WriteLine($"post: {newPost.Status}");
				posts.Add(newPost);
				return true;
			}
			catch
			{
				return false;
			}
		}
		
		private async Task<bool> CheckImageCategoryAsync(string imageUrl, string category)
		{
			List<string> acceptedEnglishTags = CategoryConverter.GetAcceptedEnglishTags(category);
			var basicAuthValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ImaggaApiKey}:{ImaggaApiSecret}"));
			var requestUrl = $"https://api.imagga.com/v2/tags?image_url={Uri.EscapeDataString(imageUrl)}";

			using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
			request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuthValue);

			try
			{
				var response = await _httpClient.SendAsync(request);

				if (!response.IsSuccessStatusCode)
				{
					// Xử lý lỗi API
					return false;
				}

				var jsonResponse = await response.Content.ReadAsStringAsync();

				// 2. Deserialize JSON
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var imaggaData = JsonSerializer.Deserialize<ImaggaResponse>(jsonResponse, options);


				Console.WriteLine($"\n--- IMAGGA TAGS FOR: {imageUrl} (Category: {category}) ---");
				var tags = imaggaData?.Result?.Tags;

				if (tags != null && tags.Any())
				{
					foreach (var tag in tags.OrderByDescending(t => t.Confidence))
					{
						// Lấy tag tiếng Anh (en) và log ra
						if (tag.Tag.TryGetValue("en", out var tagName))
						{
							Console.WriteLine($"[TAG] {tagName,-20} | Confidence: {tag.Confidence:F2}%");
						}
					}
				}
				else
				{
					Console.WriteLine("[INFO] No tags found in Imagga response.");
				}
				Console.WriteLine("----------------------------------------------------\n");


				return imaggaData?.Result?.Tags?
					.Any(tag => tag.Confidence >= ConfidenceThreshold &&
								tag.Tag.TryGetValue("en", out var tagName) &&
								acceptedEnglishTags.Contains(tagName.ToLower()))
					?? false;

			}
			catch (Exception ex)
			{
				Console.WriteLine($"[FATAL ERROR] Error processing image {imageUrl}: {ex.Message}");
				return false;
			}
		}

		public List<PostModel> GetAll()
		{
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			return posts.Select( post =>
			{
				List<DailyTimeSlots> schedule = null;
				if (!string.IsNullOrEmpty(post.ScheduleJson))
				{
					try
					{
						schedule = JsonSerializer.Deserialize<List<DailyTimeSlots>>(post.ScheduleJson, options);
					}
					catch (JsonException ex)
					{
						Console.WriteLine($"[JSON ERROR] Could not deserialize schedule for Post ID {post.Id}: {ex.Message}");
					}
				}
				var sender =  _userService.GetById(post.SenderId);
				return new PostModel
				{
					Id = post.Id,
					Name = post.Name,
					Category = post.Category,
					Description = post.Description,
					Date = post.Date,
					Address = post.Address,
					Images = post.Images,
					Status = post.Status,
					RejectMessage = post.RejectMessage,
					Sender = sender,
					Schedule = schedule
				};
			}).ToList();
		}

		public bool ApprovePost(Guid postId)
		{
			var post = posts.FirstOrDefault(p => p.Id == postId);
			if (post != null)
			{
				post.Status = "Đã Duyệt";
				return true;
			}
			return false;
		}

		public PostModel GetById(Guid id)
		{
			var post = posts.FirstOrDefault(p => p.Id == id);
			if (post == null)
			{
				return null;
			}
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			List<DailyTimeSlots> schedule = null;
			if (!string.IsNullOrEmpty(post.ScheduleJson))
			{
				try
				{
					schedule = JsonSerializer.Deserialize<List<DailyTimeSlots>>(post.ScheduleJson, options);
				}
				catch (JsonException ex)
				{
					Console.WriteLine($"[JSON ERROR] Could not deserialize schedule for Post ID {post.Id}: {ex.Message}");
				}
			}
			var sender = _userService.GetById(post.SenderId);
			if (sender != null) {
				return new PostModel
				{
					Id = post.Id,
					Name = post.Name,
					Category = post.Category,
					Description = post.Description,
					Date = post.Date,
					Address = post.Address,
					Images = post.Images,
					Status = post.Status,
					RejectMessage = post.RejectMessage,
					Sender = sender,
					Schedule = schedule
				};
			}
			return null;
		}

		public bool RejectPost(Guid postId, string rejectMessage)
		{
			var post = posts.FirstOrDefault(p => p.Id == postId);
			if (post != null)
			{
				post.Status = "Đã Từ Chối";
				post.RejectMessage = rejectMessage;
				return true;
			}
			return false;
		}

		public List<PostModel> GetPostBySenderId(Guid senderId)
		{
			var postList = posts.Where(p => p.SenderId == senderId).ToList();
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			return postList.Select(post =>
			{
				List<DailyTimeSlots> schedule = null;

				if (!string.IsNullOrEmpty(post.ScheduleJson))
				{
					try
					{
						schedule = JsonSerializer.Deserialize<List<DailyTimeSlots>>(post.ScheduleJson, options);
					}
					catch (JsonException ex)
					{
						Console.WriteLine($"[JSON ERROR] Could not deserialize schedule for Post ID {post.Id}: {ex.Message}");
					}
				}

				var sender = _userService.GetById(post.SenderId);

				return new PostModel
				{
					Id = post.Id,
					Name = post.Name,
					Category = post.Category,
					Description = post.Description,
					Date = post.Date,
					Address = post.Address,
					Images = post.Images,
					Status = post.Status,
					Sender = sender,
					RejectMessage = post.RejectMessage,
					Schedule = schedule
				};
			}).ToList();
		}

	}
}
