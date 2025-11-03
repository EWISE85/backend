using ElecWasteCollection.Application.Data;
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
using System.Xml.Linq;

namespace ElecWasteCollection.Application.Services
{
	public class PostService : IPostService
	{
		// === FIX 1: Thêm các List fake data còn thiếu ===
		private static List<Post> posts = FakeDataSeeder.posts;
		private static List<Products> products = FakeDataSeeder.products;
		private static List<ProductValues> productValues = FakeDataSeeder.productValues;
		private static List<Category> categories = FakeDataSeeder.categories;
		private static List<PostImages> postImages = FakeDataSeeder.postImages;
		private static List<SizeTier> sizeTiers = FakeDataSeeder.sizeTiers;
		private static List<Attributes> attributes = FakeDataSeeder.attributes;

		private readonly double Confidence_AcceptToSave = 30.0;

		private const string ImaggaApiKey = "acc_b80eaae365fbf2f";
		private const string ImaggaApiSecret = "ac0c2b3adc747be522c11368f95882b3";
		private const double ConfidenceThreshold = 80.0;
		private static readonly HttpClient _httpClient = new HttpClient();
		private readonly IUserService _userService;
		private readonly IProfanityChecker _profanityChecker;

		public PostService(IUserService userService, IProfanityChecker profanityChecker)
		{
			_userService = userService;
			_profanityChecker = profanityChecker;
		}

		public async Task<PostDetailModel> AddPost(CreatePostModel createPostRequest)
		{
			string postStatus = "Chờ Duyệt";
			var productRequest = createPostRequest.Product;

			if (productRequest == null)
			{
				return null; 
			}

			try
			{
				var newProduct = new Products
				{
					Id = Guid.NewGuid(),
					CategoryId = productRequest.SubCategoryId,
					Description = createPostRequest.Description,
					Status = "Chờ gom nhóm"
				};

				if (productRequest.SizeTierId.HasValue && productRequest.SizeTierId.Value != Guid.Empty)
				{
					newProduct.SizeTierId = productRequest.SizeTierId.Value;
				}
				else if (productRequest.Attributes != null && productRequest.Attributes.Any())
				{
					foreach (var attr in productRequest.Attributes)
					{
						
						double.TryParse(attr.Value, out double parsedValue);

						var newProductValue = new ProductValues
						{
							ProductValuesId = Guid.NewGuid(),
							ProductId = newProduct.Id,
							AttributeId = attr.AttributeId,
							Value = parsedValue // Sẽ lưu 0.0 nếu là chữ, an toàn
						};
						productValues.Add(newProductValue);
					}
				}

				products.Add(newProduct);

				// --- BƯỚC 3: Tạo Post (Chưa có ảnh, chưa có status) ---
				var newPost = new Post
				{
					Id = Guid.NewGuid(),
					SenderId = createPostRequest.SenderId,
					Name = createPostRequest.Name,
					Date = DateTime.Now,
					Description = string.Empty,
					Address = createPostRequest.Address,
					ScheduleJson = JsonSerializer.Serialize(createPostRequest.CollectionSchedule),
					Status = postStatus, // Sẽ cập nhật sau
					ProductId = newProduct.Id,
					CheckMessage = new List<string>()

				};


				if (createPostRequest.Images != null && createPostRequest.Images.Any())
				{
					var category = categories.FirstOrDefault(c => c.Id == productRequest.SubCategoryId);
					var categoryName = category?.Name ?? "unknown";

					var checkTasks = createPostRequest.Images
						.Select(imageUrl => CheckImageCategoryAsync(imageUrl, categoryName)) 
						.ToList();

					var results = await Task.WhenAll(checkTasks);

					// Lưu ảnh vào bảng PostImages
					for (int i = 0; i < createPostRequest.Images.Count; i++)
					{
						var imageResult = results[i];
						var newPostImage = new PostImages
						{
							PostImageId = Guid.NewGuid(),
							PostId = newPost.Id, 
							ImageUrl = createPostRequest.Images[i],
							AiDetectedLabelsJson = imageResult.DetectedTagsJson
						};
						postImages.Add(newPostImage);
					}

					if (results.All(r => r.IsMatch)) 
					{
						postStatus = "Đã Duyệt";
					}
				}
				var containsProfanity = await _profanityChecker.ContainsProfanityAsync(createPostRequest.Name);
				if (containsProfanity)
				{
					newPost.CheckMessage.Add("Tiêu đề bài đăng chứa từ ngữ không phù hợp.");
					postStatus = "Chờ Duyệt";
				}
				var containsPhoneNumber = await _profanityChecker.ContainsPhoneNumberAsync(createPostRequest.Name);
				if (containsPhoneNumber)
				{
					newPost.CheckMessage.Add("Tiêu đề bài đăng chứa số điện thoại.");
					postStatus = "Chờ Duyệt";
				}
				newPost.Status = postStatus;
				posts.Add(newPost); 

				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				return MapToPostDetailModel(newPost, options);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[FATAL ERROR] Lỗi khi tạo Post: {ex.Message}");
				return null;
			}
		}

		// lưu các tag không liên quan luôn
		private async Task<Helper.ImaggaCheckResult> CheckImageCategoryAsync(string imageUrl, string category)
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
					return new Helper.ImaggaCheckResult { IsMatch = false, DetectedTagsJson = null };
				}

				var jsonResponse = await response.Content.ReadAsStringAsync();
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var imaggaData = JsonSerializer.Deserialize<ImaggaResponse>(jsonResponse, options);
				var tags = imaggaData?.Result?.Tags;

				var allProcessedLabels = new List<LabelModel>();
				bool overallImageMatch = false;

				if (tags != null)
				{
					foreach (var tag in tags)
					{
						if (!tag.Tag.TryGetValue("en", out var tagName)) continue;

						tagName = tagName.ToLower();
						double confidence = Math.Round(tag.Confidence, 2);

						// 1. Kiểm tra xem tag có "Phù hợp" hay không
						bool isTagMatch = acceptedEnglishTags.Contains(tagName);

						// 2. Quyết định status của TOÀN BỘ ẢNH (vẫn cần ngưỡng 80%)
						if (!overallImageMatch && isTagMatch && confidence >= ConfidenceThreshold)
						{
							overallImageMatch = true;
						}

						// 3. Chỉ lưu các tag có confidence > 30% (để loại bỏ nhiễu)
						if (confidence > Confidence_AcceptToSave)
						{
							allProcessedLabels.Add(new LabelModel
							{
								Tag = tagName,
								Confidence = confidence,
								// Gán status "Phù hợp" hoặc "Không phù hợp"
								Status = isTagMatch ? "Phù hợp với danh mục" : "Không phù hợp với danh mục"
							});
						}
					}
				}

				// === PHẦN QUAN TRỌNG NHẤT (SẮP XẾP ƯU TIÊN) ===

				// Sắp xếp danh sách:
				// 1. Ưu tiên 1: Lấy các tag "Phù hợp" lên đầu
				// 2. Ưu tiên 2: Sắp xếp các tag đó theo confidence giảm dần
				var finalLabelsToShow = allProcessedLabels
					.OrderByDescending(l => l.Status == "Phù hợp với danh mục") // <-- Ưu tiên 1
					.ThenByDescending(l => l.Confidence)           // <-- Ưu tiên 2
					.Take(5) // <-- Lấy 5 tag hàng đầu (sẽ bao gồm tag "Phù hợp" trước)
					.ToList();
				// ===============================================

				return new Helper.ImaggaCheckResult
				{
					IsMatch = overallImageMatch, // Status của toàn bộ ảnh
					DetectedTagsJson = JsonSerializer.Serialize(finalLabelsToShow) // JSON của 5 tag đã ưu tiên
				};
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[FATAL ERROR] Error processing image {imageUrl}: {ex.Message}");
				return new Helper.ImaggaCheckResult { IsMatch = false, DetectedTagsJson = null };
			}
		}


		/*chỉ lưu các tag có liên quan*/

		/*private async Task<Helper.ImaggaCheckResult> CheckImageCategoryAsync(string imageUrl, string category)
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
					return new Helper.ImaggaCheckResult { IsMatch = false, DetectedTagsJson = null };
				}

				var jsonResponse = await response.Content.ReadAsStringAsync();
				var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				var imaggaData = JsonSerializer.Deserialize<ImaggaResponse>(jsonResponse, options);

				var tags = imaggaData?.Result?.Tags;

				var processedLabels = new List<LabelModel>();
				bool overallImageMatch = false;

				if (tags != null)
				{

					var relevantTags = new List<LabelModel>();

					foreach (var tag in tags)
					{
						if (!tag.Tag.TryGetValue("en", out var tagName)) continue;

						tagName = tagName.ToLower();
						double confidence = tag.Confidence;

						bool isTagMatch = acceptedEnglishTags.Contains(tagName);

						if (isTagMatch && confidence > 30.0)
						{
							relevantTags.Add(new LabelModel
							{
								Tag = tagName,
								Confidence = Math.Round(confidence, 2),
								Status = "Phù hợp" // Bây giờ 100% sẽ là "Phù hợp"
							});

							// Kiểm tra ngưỡng 80% để quyết định duyệt Post
							if (!overallImageMatch && confidence >= ConfidenceThreshold)
							{
								overallImageMatch = true;
							}
						}
					}

					// 4. Sắp xếp và Giới hạn:
					// Lấy Top 5 tag "Phù hợp" có confidence cao nhất
					processedLabels = relevantTags
						.OrderByDescending(l => l.Confidence)
						.Take(5) // Chỉ lấy 5 tag liên quan nhất
						.ToList();
				}

				return new Helper.ImaggaCheckResult
				{
					IsMatch = overallImageMatch,
					DetectedTagsJson = JsonSerializer.Serialize(processedLabels) // Chỉ lưu 5 tag "Phù hợp"
				};
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[FATAL ERROR] Error processing image {imageUrl}: {ex.Message}");
				return new Helper.ImaggaCheckResult { IsMatch = false, DetectedTagsJson = null };
			}
		}*/


		public List<PostSummaryModel> GetAll()
		{
			return posts.Select(post => MapToPostSummaryModel(post)).ToList();
		}

		public PostDetailModel GetById(Guid id)
		{
			var post = posts.FirstOrDefault(p => p.Id == id);
			if (post == null)
			{
				return null;
			}
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			return MapToPostDetailModel(post, options);
		}

		public List<PostDetailModel> GetPostBySenderId(Guid senderId)
		{
			var postList = posts.Where(p => p.SenderId == senderId).ToList();
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			return postList.Select(post => MapToPostDetailModel(post, options)).ToList();
		}

		// === SỬA LẠI: MapToPostModel (để lấy ảnh từ bảng PostImages) ===
		private PostSummaryModel MapToPostSummaryModel(Post post)
		{
			if (post == null) return null;

			var sender = _userService.GetById(post.SenderId);
			var product = products.FirstOrDefault(p => p.Id == post.ProductId);
			var category = categories.FirstOrDefault(c => c.Id == product?.CategoryId);

			// Lấy ảnh đầu tiên làm thumbnail
			var thumbnailUrl = postImages
				.FirstOrDefault(pi => pi.PostId == post.Id)?
				.ImageUrl;

			return new PostSummaryModel
			{
				Id = post.Id,
				Name = post.Name,
				Category = category?.Name ?? "Không rõ",
				Status = post.Status,
				Date = post.Date,
				Address = post.Address,
				SenderName = sender?.Name ?? "Không rõ",
				ThumbnailUrl = thumbnailUrl
			};
		}

		private PostDetailModel MapToPostDetailModel(Post post, JsonSerializerOptions options)
		{
			if (post == null) return null;

			var sender = _userService.GetById(post.SenderId);
			var product = products.FirstOrDefault(p => p.Id == post.ProductId);

			string categoryName = "Không rõ";
			var productDetailModel = new ProductDetailModel(); // Model lồng
			var parentCategoryId = categories.FirstOrDefault(c => c.Id == product.CategoryId)?.ParentCategoryId;
			var parentCategory = categories.FirstOrDefault(c => c.Id == parentCategoryId);

			if (product != null)
			{
				var category = categories.FirstOrDefault(c => c.Id == product.CategoryId);
				categoryName = category?.Name ?? "Không rõ";

				// Xây dựng ProductDetailModel
				productDetailModel.ProductId = product.Id;
				productDetailModel.Description = product.Description;

				// Kiểm tra xem người dùng dùng SizeTier hay Attributes
				if (product.SizeTierId.HasValue)
				{
					productDetailModel.SizeTierName = sizeTiers
						.FirstOrDefault(st => st.SizeTierId == product.SizeTierId.Value)?.Name;
				}
				else
				{
					// Lấy danh sách Attributes chi tiết
					productDetailModel.Attributes = productValues
						.Where(pv => pv.ProductId == product.Id)
						.Join(attributes,
							  pv => pv.AttributeId,
							  attr => attr.Id,
							  (pv, attr) => new ProductValueDetailModel
							  {
								  AttributeName = attr.Name,
								  Value = pv.Value.ToString(),
								  // GHI CHÚ: Bảng Attributes của bạn thiếu Unit
								  Unit = ""
							  })
						.ToList();
				}
			}

			// Deserialize Lịch hẹn
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
			var allPostImages = postImages.Where(pi => pi.PostId == post.Id).ToList();
			var imageUrls = allPostImages.Select(pi => pi.ImageUrl).ToList();
			var allLabels = new List<LabelModel>();
			foreach (var img in allPostImages)
			{
				var labelsFromThisImage = JsonSerializer.Deserialize<List<LabelModel>>(img.AiDetectedLabelsJson ?? "[]", options);
				allLabels.AddRange(labelsFromThisImage);
			}
			var aggregatedLabels = allLabels
		.GroupBy(l => l.Tag) // Nhóm các tag trùng tên (ví dụ: "toy", "toy")
		.Select(group => new LabelModel
		{
			Tag = group.Key,
			// Lấy confidence CAO NHẤT trong nhóm
			Confidence = group.Max(g => g.Confidence),
			// Lấy status đầu tiên (vì chúng giống nhau)
			Status = group.First().Status
		})
		.OrderByDescending(l => l.Confidence) // Sắp xếp theo cái cao nhất
		.Take(5) // <-- Chỉ lấy 5 tag HÀNG ĐẦU TỔNG CỘNG
		.ToList();

			// 4. Tạo PostModel chi tiết
			return new PostDetailModel
			{
				Id = post.Id,
				Name = post.Name,
				ParentCategory = parentCategory.Name,
				SubCategory = categoryName,
				Status = post.Status,
				RejectMessage = post.RejectMessage,
				Date = post.Date,
				Address = post.Address,
				Sender = sender,
				Schedule = schedule,
				PostNote = post.Description,
				Product = productDetailModel,
				CheckMessage = post.CheckMessage,


				// === GÁN KẾT QUẢ MỚI ===
				ImageUrls = imageUrls,
				AggregatedAiLabels = aggregatedLabels
			};
		}
		public async Task<bool> ApprovePost(Guid postId)
		{
			var post = posts.FirstOrDefault(p => p.Id == postId);
			if (post != null)
			{
				post.Name = await _profanityChecker.CensorTextAsync(post.Name);
				post.Status = "Đã Duyệt";
				return true;
			}
			return false;
		}

		public async Task<bool> RejectPost(Guid postId, string rejectMessage)
		{
			var checkBadWord = await _profanityChecker.ContainsProfanityAsync(rejectMessage);
			if (checkBadWord)
			{
				return false;
			}
			var post = posts.FirstOrDefault(p => p.Id == postId);
			if (post != null)
			{
				post.Status = "Đã Từ Chối";
				post.RejectMessage = rejectMessage;
				var product = products.FirstOrDefault(p => p.Id == post.ProductId);
				if (product != null)
				{
					product.Status = "Đã Từ Chối";
				}
				return true;
			}
			return false;
		}
	}
}