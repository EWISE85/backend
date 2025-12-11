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
		//private static List<PostImages> postImages = FakeDataSeeder.postImages;
		private static List<Attributes> attributes = FakeDataSeeder.attributes;
		private static List<Brand> _brands = FakeDataSeeder.brands;
		private static List<ProductStatusHistory> _productStatusHistories = FakeDataSeeder.productStatusHistories;
		private static List<ProductImages> _productImages = FakeDataSeeder.productImages;

		
		private static readonly HttpClient _httpClient = new HttpClient();
		private readonly IUserService _userService;
		private readonly IProfanityChecker _profanityChecker;
		private readonly IProductService _productService;
		private readonly IImageRecognitionService _imageRecognitionService;

		public PostService(IUserService userService, IProfanityChecker profanityChecker, IProductService productService, IImageRecognitionService imageRecognitionService)
		{
			_userService = userService;
			_profanityChecker = profanityChecker;
			_productService = productService;
			_imageRecognitionService = imageRecognitionService;
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
					ProductId = Guid.NewGuid(),
					CategoryId = productRequest.SubCategoryId,
					BrandId = productRequest.BrandId,
					Description = createPostRequest.Description,
					CreateAt = DateOnly.FromDateTime(DateTime.Now.AddHours(7)),
					isChecked = false,
					Status = "Chờ Duyệt"
				};
				foreach (var attr in productRequest.Attributes)
				{



					var newProductValue = new ProductValues
					{
						ProductValuesId = Guid.NewGuid(),
						ProductId = newProduct.ProductId,
						AttributeId = attr.AttributeId,
						AttributeOptionId = attr.OptionId,
						Value = attr.Value
					};
					productValues.Add(newProductValue);
				}




				// --- BƯỚC 3: Tạo Post (Chưa có ảnh, chưa có status) ---
				var newPost = new Post
				{
					PostId = Guid.NewGuid(),
					SenderId = createPostRequest.SenderId,
					Date = DateTime.Now,
					Description = string.Empty,
					Address = createPostRequest.Address,
					ScheduleJson = JsonSerializer.Serialize(createPostRequest.CollectionSchedule),
					Status = postStatus, // Sẽ cập nhật sau
					ProductId = newProduct.ProductId,
					EstimatePoint = 50,
					CheckMessage = new List<string>()

				};


				if (createPostRequest.Images != null && createPostRequest.Images.Any())
				{
					var category = categories.FirstOrDefault(c => c.CategoryId == productRequest.SubCategoryId);
					var categoryName = category?.Name ?? "unknown";

					var checkTasks = createPostRequest.Images
						.Select(async imageUrl => await _imageRecognitionService.AnalyzeImageCategoryAsync(imageUrl, categoryName))
						.ToList();

					var results = await Task.WhenAll(checkTasks);

					for (int i = 0; i < createPostRequest.Images.Count; i++)
					{
						var imageResult = results[i];
						var newPostImage = new ProductImages
						{
							ProductImagesId = Guid.NewGuid(),
							ProductId = newProduct.ProductId,
							ImageUrl = createPostRequest.Images[i],
							AiDetectedLabelsJson = imageResult.DetectedTagsJson
						};
						_productImages.Add(newPostImage);
					}

					if (results.All(r => r.IsMatch))
					{
						postStatus = "Đã Duyệt";
						newProduct.Status = "Chờ gom nhóm";
						var history = new ProductStatusHistory
						{
							ProductId = newProduct.ProductId,
							ChangedAt = DateTime.Now.AddHours(7),
							Status = "Chờ gom nhóm",
							StatusDescription = "Yêu cầu được duyệt"
						};
						products.Add(newProduct);
						_productStatusHistories.Add(history);
					}
					else
					{
						var history = new ProductStatusHistory
						{
							ProductId = newProduct.ProductId,
							ChangedAt = DateTime.Now.AddHours(7),
							Status = "Chờ Duyệt",
							StatusDescription = "Yêu cầu đã được gửi"
						};
						products.Add(newProduct);
						_productStatusHistories.Add(history);
					}



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

		
		public List<PostSummaryModel> GetAll()
		{
			return posts.Select(post => MapToPostSummaryModel(post)).ToList();
		}

		public PostDetailModel GetById(Guid id)
		{
			var post = posts.FirstOrDefault(p => p.PostId == id);
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
			var product = products.FirstOrDefault(p => p.ProductId == post.ProductId);

			// Bước 1: Lấy category được gán trực tiếp (VD: "Tivi")
			var directCategory = categories.FirstOrDefault(c => c.CategoryId == product?.CategoryId);

			string finalCategoryName = "Không rõ";

			if (directCategory != null)
			{
				// Bước 2: Kiểm tra xem category này có cha không?
				if (directCategory.ParentCategoryId != null)
				{
					// Bước 3: Nếu có cha, tìm và lấy tên của cha
					var parentCategory = categories.FirstOrDefault(c => c.CategoryId == directCategory.ParentCategoryId);
					if (parentCategory != null)
					{
						finalCategoryName = parentCategory.Name; // Chỉ lấy tên cha
					}
					else
					{
						// Trường hợp hiếm: có ParentCategoryId nhưng không tìm thấy cha
						finalCategoryName = "Lỗi (Không tìm thấy cha)";
					}
				}
				else
				{
					// Bước 4: Nếu không có cha (nó đã là cha), thì lấy chính nó
					finalCategoryName = directCategory.Name;
				}
			}

			// Lấy ảnh
			var thumbnailUrl = _productImages
				.FirstOrDefault(pi => pi.ProductId == post.ProductId)?
				.ImageUrl;

			return new PostSummaryModel
			{
				Id = post.PostId,
				Category = finalCategoryName, // <--- SỬ DỤNG TÊN ĐÃ QUA XỬ LÝ
				Status = post.Status,
				Date = post.Date,
				Address = post.Address,
				SenderName = sender?.Name ?? "Không rõ",
				ThumbnailUrl = thumbnailUrl,
				EstimatePoint = post.EstimatePoint
			};
		}

		private PostDetailModel MapToPostDetailModel(Post post, JsonSerializerOptions options)
		{
			if (post == null) return null;

			var sender = _userService.GetById(post.SenderId);
			var userRespose = new UserResponse
			{
				UserId = sender.UserId,
				Avatar = sender.Avatar,
				Email = sender.Email,
				Name = sender.Name,
				Phone = sender.Phone,
				Role = sender.Role,
				SmallCollectionPointId = sender.SmallCollectionPointId
			};
			var product = products.FirstOrDefault(p => p.ProductId == post.ProductId);

			string categoryName = "Không rõ";
			var productDetailModel = new ProductDetailModel(); // Model lồng
			var parentCategoryId = categories.FirstOrDefault(c => c.CategoryId == product.CategoryId)?.ParentCategoryId;
			var parentCategory = categories.FirstOrDefault(c => c.CategoryId == parentCategoryId);

			if (product != null)
			{
				var category = categories.FirstOrDefault(c => c.CategoryId == product.CategoryId);
				categoryName = category?.Name ?? "Không rõ";
				var brand = _brands.FirstOrDefault(b => b.BrandId == product.BrandId);
				// Xây dựng ProductDetailModel
				productDetailModel.ProductId = product.ProductId;
				productDetailModel.Description = product.Description;
				productDetailModel.BrandId = product.BrandId;
				productDetailModel.BrandName = brand?.Name ?? "Không rõ";

				// Kiểm tra xem người dùng dùng SizeTier hay Attributes

				// Lấy danh sách Attributes chi tiết
				productDetailModel.Attributes = productValues
					.Where(pv => pv.ProductId == product.ProductId)
					.Join(attributes,
						  pv => pv.AttributeId,
						  attr => attr.AttributeId,
						  (pv, attr) => new ProductValueDetailModel
						  {
							  AttributeName = attr.Name,
							  AttributeId = attr.AttributeId,
							  OptionId = pv.AttributeOptionId,
							  OptionName = attr.AttributeOptions?
					.FirstOrDefault(o => o.OptionId == pv.AttributeOptionId)?
					.OptionName,
							  Value = pv.Value.ToString(),
						  })
					.ToList();

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
					Console.WriteLine($"[JSON ERROR] Could not deserialize schedule for Post ID {post.PostId}: {ex.Message}");
				}
			}
			var allProductImages = _productImages.Where(pi => pi.ProductId == post.ProductId).ToList();
			var imageUrls = allProductImages.Select(pi => pi.ImageUrl).ToList();
			var allLabels = new List<LabelModel>();
			foreach (var img in allProductImages)
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
			var userResponse = new UserResponse
			{
				UserId = sender.UserId,
				Avatar = sender.Avatar,
				Email = sender.Email,
				Name = sender.Name,
				Phone = sender.Phone,
				Role = sender.Role,
				SmallCollectionPointId = sender.SmallCollectionPointId
			};
			// 4. Tạo PostModel chi tiết
			return new PostDetailModel
			{
				Id = post.PostId,
				//Name = post.Name,
				ParentCategory = parentCategory.Name,
				SubCategory = categoryName,
				Status = post.Status,
				RejectMessage = post.RejectMessage,
				Date = post.Date,
				Address = post.Address,
				Sender = userResponse,
				Schedule = schedule,
				PostNote = post.Description,
				Product = productDetailModel,
				CheckMessage = post.CheckMessage,
				EstimatePoint = post.EstimatePoint,

				// === GÁN KẾT QUẢ MỚI ===
				ImageUrls = imageUrls,
				AggregatedAiLabels = aggregatedLabels
			};
		}
		public async Task<bool> ApprovePost(Guid postId)
		{
			var post = posts.FirstOrDefault(p => p.PostId == postId);
			if (post != null)
			{
				post.Status = "Đã Duyệt";
				
				var product = products.FirstOrDefault(p => p.ProductId == post.ProductId);
				if (product != null)
				{
					product.Status = "Chờ gom nhóm";
					_productService.UpdateProductStatusByProductId(product.ProductId, product.Status);
					var history = new ProductStatusHistory
					{
						ProductId = post.ProductId,
						ChangedAt = DateTime.Now,
						Status = "Chờ gom nhóm",
						StatusDescription = "Yêu cầu được duyệt và chờ gom nhóm"
					};
					_productStatusHistories.Add(history);

				}
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
			var post = posts.FirstOrDefault(p => p.PostId == postId);
			if (post != null)
			{
				post.Status = "Đã Từ Chối";
				post.RejectMessage = rejectMessage;
				var product = products.FirstOrDefault(p => p.ProductId == post.ProductId);
				if (product != null)
				{
					product.Status = "Đã Từ Chối";
				}
				return true;
			}
			return false;
		}

		public Task<PagedResultModel<PostSummaryModel>> GetPagedPostsAsync(PostSearchQueryModel model)
		{

			var queryablePosts = posts.Select(post =>
			{
				var product = products.FirstOrDefault(p => p.ProductId == post.ProductId);
				var directCategory = categories.FirstOrDefault(c => c.CategoryId == product?.CategoryId);
				string parentCategoryName = null;

				if (directCategory != null)
				{
					if (directCategory.ParentCategoryId != null)
					{
						var parentCategory = categories.FirstOrDefault(c => c.CategoryId == directCategory.ParentCategoryId);
						parentCategoryName = parentCategory?.Name;
					}
					else
					{
						parentCategoryName = directCategory.Name; // Nó chính là category cha
					}
				}

				return new
				{
					PostObject = post,
					SearchableCategoryName = parentCategoryName ?? ""
				};
			}).AsQueryable();

			// Bước 2: Lọc theo Status (nếu có)
			if (!string.IsNullOrEmpty(model.Status))
			{
				queryablePosts = queryablePosts.Where(p => p.PostObject.Status == model.Status);
			}

			// Bước 3: Lọc theo Search (Name bài đăng HOẶC Name category cha)
			if (!string.IsNullOrEmpty(model.Search))
			{
				string searchLower = model.Search.ToLower();
				queryablePosts = queryablePosts.Where(p =>
					p.SearchableCategoryName.ToLower().Contains(searchLower)
				);
			}

			// Bước 4: Lấy tổng số lượng (trước khi phân trang)
			int totalItems = queryablePosts.Count();

			// Bước 5: Sắp xếp (ORDER BY)
			// Áp dụng logic nghiệp vụ bạn yêu cầu
			IQueryable<dynamic> sortedPosts;
			if (model.Status == "Chờ Duyệt")
			{
				// "nếu status là chờ duyệt thì orderby ngày tăng dần"
				sortedPosts = queryablePosts.OrderBy(p => p.PostObject.Date);
			}
			else if (model.Status == "Đã Duyệt" || model.Status == "Đã Từ Chối")
			{
				// "nếu status là đã duyệt, đã từ chối thì order ngày giảm dần"
				sortedPosts = queryablePosts.OrderByDescending(p => p.PostObject.Date);
			}
			else
			{
				// Trường hợp không lọc status (hoặc status khác),
				// thì tuân theo tham số 'order'
				if (model.Order?.ToUpper() == "ASC")
				{
					sortedPosts = queryablePosts.OrderBy(p => p.PostObject.Date);
				}
				else
				{
					// Mặc định là giảm dần (DESC)
					sortedPosts = queryablePosts.OrderByDescending(p => p.PostObject.Date);
				}
			}

			// Bước 6: Phân trang (SKIP & TAKE)
			var pagedData = sortedPosts
				.Skip((model.Page - 1) * model.Limit)
				.Take(model.Limit)
				.ToList(); // Thực thi query (lấy dữ liệu)

			// Bước 7: Map kết quả sang PostSummaryModel
			var finalResultList = pagedData
	.Select(p => MapToPostSummaryModel(p.PostObject))
	.Cast<PostSummaryModel>() // <-- THÊM DÒNG NÀY
	.ToList();

			// Bước 8: Tạo PagedResultModel
			var pagedResult = new PagedResultModel<PostSummaryModel>(
				finalResultList,
				model.Page,
				model.Limit,
				totalItems
			);

			// Trả về Task (vì đang dùng fake list)
			return Task.FromResult(pagedResult);
		}


	}
}