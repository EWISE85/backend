using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Google.Apis.Logging;
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
		private readonly IProfanityChecker _profanityChecker;
		private readonly IProductService _productService;
		private readonly IImageRecognitionService _imageRecognitionService;
		private readonly IProductRepository _productRepository;
		private readonly IProductImageRepository _productImageRepository;
		private readonly IProductValuesRepository _productValuesRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IPostRepository _postRepository;
		private readonly IProductStatusHistoryRepository _productStatusHistoryRepository;
		private readonly ICategoryRepository _categoryRepository;
		private readonly IAttributeRepository _attributeRepository;
		private readonly IAttributeOptionRepository _attributeOptionRepository;
        private readonly IMapboxDistanceCacheService _distanceCache;

        public PostService(IProfanityChecker profanityChecker, IProductService productService, IImageRecognitionService imageRecognitionService, IProductImageRepository productImageRepository, IProductRepository productRepository, IProductValuesRepository productValuesRepository, IUnitOfWork unitOfWork, IPostRepository postRepository, IProductStatusHistoryRepository productStatusHistoryRepository, ICategoryRepository categoryRepository, IAttributeRepository attributeRepository, IAttributeOptionRepository attributeOptionRepository, IMapboxDistanceCacheService distanceCache)
		{
			_profanityChecker = profanityChecker;
			_productService = productService;
			_imageRecognitionService = imageRecognitionService;
			_productImageRepository = productImageRepository;
			_productRepository = productRepository;
			_productValuesRepository = productValuesRepository;
			_unitOfWork = unitOfWork;
			_postRepository = postRepository;
			_productStatusHistoryRepository = productStatusHistoryRepository;
			_categoryRepository = categoryRepository;
			_attributeRepository = attributeRepository;
			_attributeOptionRepository = attributeOptionRepository;
            _distanceCache = distanceCache;
        }

        public async Task<bool> AddPost(CreatePostModel createPostRequest)
        {
            if (createPostRequest.Product == null) throw new AppException("Product đang trống", 400);

            var userLocation = await _unitOfWork.UserAddresses.GetAsync(
                a => a.UserId == createPostRequest.SenderId && a.Address == createPostRequest.Address);

            if (userLocation == null || !userLocation.Iat.HasValue || !userLocation.Ing.HasValue)
            {
                throw new AppException("Địa chỉ không hợp lệ hoặc chưa được định vị trên bản đồ.", 400);
            }

            bool isServiceable = await CheckIfLocationAndCategoryAreServiceable(
                createPostRequest.Product.SubCategoryId,
                userLocation.Iat.Value,
                userLocation.Ing.Value);

            if (!isServiceable)
            {
                throw new AppException("Rất tiếc, loại hàng này hiện chưa được hỗ trợ thu gom tại khu vực của bạn.", 400);
            }

            DateTime transactionTimeUtc = DateTime.UtcNow;
            try
            {
                var validationRules = await _unitOfWork.CategoryAttributes.GetsAsync(
                                        x => x.CategoryId == createPostRequest.Product.SubCategoryId,
                                        includeProperties: "Attribute");

                string currentStatus = PostStatus.CHO_DUYET.ToString();
                string currentProductStatus = ProductStatus.CHO_DUYET.ToString();
                string statusDescription = "Yêu cầu đã được gửi";
                Guid newProductId = Guid.NewGuid();

                var newProduct = new Products
                {
                    ProductId = newProductId,
                    CategoryId = createPostRequest.Product.SubCategoryId,
                    BrandId = createPostRequest.Product.BrandId,
                    Description = createPostRequest.Description,
                    CreateAt = DateOnly.FromDateTime(transactionTimeUtc),
                    UserId = createPostRequest.SenderId,
                    isChecked = false,
                    Status = currentProductStatus
                };

                if (createPostRequest.Product.Attributes != null)
                {
                    foreach (var attr in createPostRequest.Product.Attributes)
                    {
                        var rule = validationRules.FirstOrDefault(x => x.AttributeId == attr.AttributeId);
                        if (attr.OptionId == null && attr.Value.HasValue && rule != null)
                        {
                            if (rule.MinValue.HasValue && attr.Value.Value < rule.MinValue.Value)
                            {
                                throw new AppException($"Giá trị của '{rule.Attribute.Name}' quá nhỏ. Tối thiểu phải là {rule.MinValue} {rule.Unit}.", 400);
                            }
                        }
                        var newProductValue = new ProductValues
                        {
                            ProductValuesId = Guid.NewGuid(),
                            ProductId = newProductId,
                            AttributeId = attr.AttributeId,
                            AttributeOptionId = attr.OptionId,
                            Value = attr.Value
                        };
                        await _unitOfWork.ProductValues.AddAsync(newProductValue);
                    }
                }

                if (createPostRequest.Images != null && createPostRequest.Images.Any())
                {
                    var category = await _categoryRepository.GetByIdAsync(createPostRequest.Product.SubCategoryId);
                    var categoryName = category?.Name ?? "unknown";
                    bool allImagesMatch = true;

                    foreach (var imgUrl in createPostRequest.Images)
                    {
                        var aiResult = await _imageRecognitionService.AnalyzeImageCategoryAsync(imgUrl, categoryName);
                        if (aiResult == null || !aiResult.IsMatch) allImagesMatch = false;

                        var productImg = new ProductImages
                        {
                            ProductImagesId = Guid.NewGuid(),
                            ProductId = newProductId,
                            ImageUrl = imgUrl,
                            AiDetectedLabelsJson = aiResult?.DetectedTagsJson ?? "[]"
                        };
                        await _unitOfWork.ProductImages.AddAsync(productImg);
                    }

                    if (allImagesMatch)
                    {
                        currentStatus = PostStatus.DA_DUYET.ToString();
                        newProduct.Status = ProductStatus.CHO_PHAN_KHO.ToString();
                        statusDescription = "Yêu cầu được duyệt tự động, chờ phân về kho tương ứng";
                    }
                }

                var history = new ProductStatusHistory
                {
                    ProductId = newProductId,
                    ChangedAt = DateTime.UtcNow,
                    Status = newProduct.Status,
                    StatusDescription = statusDescription
                };

                var newPost = new Post
                {
                    PostId = Guid.NewGuid(),
                    SenderId = createPostRequest.SenderId,
                    Date = DateTime.UtcNow,
                    Description = createPostRequest.Description,
                    Address = createPostRequest.Address,
                    ScheduleJson = JsonSerializer.Serialize(createPostRequest.CollectionSchedule),
                    Status = currentStatus,
                    ProductId = newProductId,
                    EstimatePoint = 50,
                    CheckMessage = new List<string>()
                };

                await _unitOfWork.Products.AddAsync(newProduct);
                await _unitOfWork.ProductStatusHistory.AddAsync(history);
                await _unitOfWork.Posts.AddAsync(newPost);

                await _unitOfWork.SaveAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL ERROR] AddPost: {ex}");
                throw;
            }
        }

        private async Task<bool> CheckIfLocationAndCategoryAreServiceable(Guid subCategoryId, double userLat, double userLng)
        {
            var allCategories = await _unitOfWork.Categories.GetAllAsync();
            var category = allCategories.FirstOrDefault(c => c.CategoryId == subCategoryId);
            Guid rootCateId = category?.ParentCategoryId ?? subCategoryId;

            var allCompanies = await _unitOfWork.Companies.GetAllAsync(includeProperties: "SmallCollectionPoints,CompanyRecyclingCategories");
            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();
            var recyclingCompanies = allCompanies.Where(c => c.CompanyType == CompanyType.CTY_TAI_CHE.ToString()).ToList();

            var validCandidates = new List<SmallCollectionPoints>();

            foreach (var comp in allCompanies.Where(c => c.CompanyType == CompanyType.CTY_THU_GOM.ToString()))
            {
                foreach (var sp in comp.SmallCollectionPoints.Where(s => s.Status == CompanyStatus.DANG_HOAT_DONG.ToString()))
                {
                    var rComp = recyclingCompanies.FirstOrDefault(c => c.CompanyId == sp.RecyclingCompanyId);
                    bool canHandle = rComp?.CompanyRecyclingCategories.Any(crc => crc.CategoryId == rootCateId) ?? false;

                    if (!canHandle) continue;

                    double hvDist = GeoHelper.DistanceKm(sp.Latitude, sp.Longitude, userLat, userLng);
                    double radius = GetConfigValue(allConfigs, null, sp.SmallCollectionPointsId, SystemConfigKey.RADIUS_KM, 10);

                    if (hvDist <= radius)
                    {
                        validCandidates.Add(sp);
                    }
                }
            }

            if (!validCandidates.Any()) return false;

            var roadDistances = await _distanceCache.GetMatrixDistancesAsync(userLat, userLng, validCandidates);

            foreach (var point in validCandidates)
            {
                double maxRoad = GetConfigValue(allConfigs, null, point.SmallCollectionPointsId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, 15);
                double hvDist = GeoHelper.DistanceKm(point.Latitude, point.Longitude, userLat, userLng);

                if (roadDistances.TryGetValue(point.SmallCollectionPointsId, out double roadKm))
                {
                    if (roadKm <= maxRoad) return true;
                }
                else
                {

                    if (hvDist <= maxRoad)
                    {
                        Console.WriteLine($"[AddPost Fallback] Mapbox Error. Approved by Haversine: {hvDist}km for Point: {point.SmallCollectionPointsId}");
                        return true;
                    }
                }
            }

            return false;
        }

        private double GetConfigValue(IEnumerable<SystemConfig> configs, string? companyId, string? pointId, SystemConfigKey key, double defaultValue)
        {
            var cfg = configs.FirstOrDefault(c => c.Key == key.ToString() && c.CompanyId == companyId && c.SmallCollectionPointId == pointId)
                   ?? configs.FirstOrDefault(c => c.Key == key.ToString() && c.CompanyId == companyId && c.SmallCollectionPointId == null)
                   ?? configs.FirstOrDefault(c => c.Key == key.ToString() && c.CompanyId == null && c.SmallCollectionPointId == null);

            return cfg != null && double.TryParse(cfg.Value, out double val) ? val : defaultValue;
        }

        //public async Task<bool> AddPost(CreatePostModel createPostRequest)
        //{

        //	if (createPostRequest.Product == null) throw new AppException("Product đang trống", 400);
        //	//if (createPostRequest.Product.Attributes == null || !createPostRequest.Product.Attributes.Any()) throw new AppException("Thuộc tính sản phẩm đang trống", 400);
        //	DateTime transactionTimeUtc = DateTime.UtcNow;
        //	try
        //	{
        //		var validationRules = await _unitOfWork.CategoryAttributes.GetsAsync(
        //								x => x.CategoryId == createPostRequest.Product.SubCategoryId,
        //								includeProperties: "Attribute");
        //		string currentStatus = PostStatus.CHO_DUYET.ToString();
        //		string currentProductStatus = ProductStatus.CHO_DUYET.ToString();
        //		string statusDescription = "Yêu cầu đã được gửi";
        //		Guid newProductId = Guid.NewGuid();

        //		var newProduct = new Products
        //		{
        //			ProductId = newProductId,
        //			CategoryId = createPostRequest.Product.SubCategoryId,
        //			BrandId = createPostRequest.Product.BrandId,
        //			Description = createPostRequest.Description,
        //			CreateAt = DateOnly.FromDateTime(transactionTimeUtc),
        //			UserId = createPostRequest.SenderId,
        //			isChecked = false,
        //			Status = currentProductStatus
        //		};

        //		if (createPostRequest.Product.Attributes != null)
        //		{
        //			foreach (var attr in createPostRequest.Product.Attributes)
        //			{
        //				var rule = validationRules.FirstOrDefault(x => x.AttributeId == attr.AttributeId);
        //				if (attr.OptionId == null && attr.Value.HasValue && rule != null)
        //				{
        //					if (rule.MinValue.HasValue && attr.Value.Value < rule.MinValue.Value)
        //					{
        //						throw new AppException($"Giá trị của '{rule.Attribute.Name}' quá nhỏ. Tối thiểu phải là {rule.MinValue} {rule.Unit}.", 400);
        //					}
        //					//if (rule.MaxValue.HasValue && attr.Value.Value > rule.MaxValue.Value)
        //					//{
        //					//	throw new AppException($"Giá trị của '{rule.Attribute.Name}' quá lớn. Tối đa chỉ được {rule.MaxValue} {rule.Unit}.", 400);
        //					//}
        //				}
        //				var newProductValue = new ProductValues
        //				{
        //					ProductValuesId = Guid.NewGuid(),
        //					ProductId = newProductId,
        //					AttributeId = attr.AttributeId,
        //					AttributeOptionId = attr.OptionId,
        //					Value = attr.Value
        //				};

        //				await _unitOfWork.ProductValues.AddAsync(newProductValue);

        //			}
        //		}


        //		if (createPostRequest.Images != null && createPostRequest.Images.Any())
        //		{
        //			var category = await _categoryRepository.GetByIdAsync(createPostRequest.Product.SubCategoryId);
        //			var categoryName = category?.Name ?? "unknown";

        //			bool allImagesMatch = true; 

        //			foreach (var imgUrl in createPostRequest.Images)
        //			{
        //				var aiResult = await _imageRecognitionService.AnalyzeImageCategoryAsync(imgUrl, categoryName);

        //				if (aiResult == null || !aiResult.IsMatch)
        //				{
        //					allImagesMatch = false;
        //				}

        //				var productImg = new ProductImages
        //				{
        //					ProductImagesId = Guid.NewGuid(),
        //					ProductId = newProductId,
        //					ImageUrl = imgUrl,
        //					AiDetectedLabelsJson = aiResult?.DetectedTagsJson ?? "[]"
        //				};

        //				await _unitOfWork.ProductImages.AddAsync(productImg);
        //			}

        //			if (allImagesMatch)
        //			{
        //				currentStatus = PostStatus.DA_DUYET.ToString();
        //				newProduct.Status = ProductStatus.CHO_PHAN_KHO.ToString();
        //				statusDescription = "Yêu cầu được duyệt tự động, chờ phân về kho tương ứng";
        //			}
        //		}


        //		if (newProduct.Status == ProductStatus.CHO_DUYET.ToString())
        //		{
        //			statusDescription = "Yêu cầu đã được gửi.";
        //		}

        //		var history = new ProductStatusHistory
        //		{
        //			ProductId = newProductId,
        //			ChangedAt = DateTime.UtcNow,
        //			Status = newProduct.Status, 
        //			StatusDescription = statusDescription
        //		};

        //		var newPost = new Post
        //		{
        //			PostId = Guid.NewGuid(),
        //			SenderId = createPostRequest.SenderId,
        //			Date = DateTime.UtcNow,
        //			Description = createPostRequest.Description, 
        //			Address = createPostRequest.Address,
        //			ScheduleJson = JsonSerializer.Serialize(createPostRequest.CollectionSchedule),
        //			Status = currentStatus,
        //			ProductId = newProductId,
        //			EstimatePoint = 50, 
        //			CheckMessage = new List<string>() 
        //		};

        //		await _unitOfWork.Products.AddAsync(newProduct);
        //		await _unitOfWork.ProductStatusHistory.AddAsync(history);
        //		await _unitOfWork.Posts.AddAsync(newPost);


        //		await _unitOfWork.SaveAsync();

        //		return true;
        //	}
        //	catch (Exception ex)
        //	{
        //		Console.WriteLine($"[FATAL ERROR] AddPost: {ex}");
        //		throw;
        //	}
        //}


        public async Task<List<PostSummaryModel>> GetAll()
		{
			var posts = await _postRepository.GetAllPostsWithDetailsAsync();

			if (posts == null) return new List<PostSummaryModel>();

			return posts.Select(post => MapToPostSummaryModel(post)).ToList();
		}

		public async Task<PostDetailModel> GetById(Guid id)
		{
			var post = await _postRepository.GetPostWithDetailsAsync(id);
			if (post == null) return null;
			var productValues = post.Product?.ProductValues ?? new List<ProductValues>();

			var attrIds = productValues.Select(pv => pv.AttributeId).Distinct().ToList();
			var optionIds = productValues.Where(pv => pv.AttributeOptionId.HasValue)
										 .Select(pv => pv.AttributeOptionId.Value)
										 .Distinct().ToList();
			var attributes = await _attributeRepository.GetsAsync(a => attrIds.Contains(a.AttributeId));
			var optionsList = await _attributeOptionRepository.GetsAsync(o => optionIds.Contains(o.OptionId));
			var attrDict = attributes.ToDictionary(k => k.AttributeId, v => v.Name);
			var optionDict = optionsList.ToDictionary(k => k.OptionId, v => v.OptionName);
			var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			return MapToPostDetailModel(post, attrDict, optionDict, jsonOptions);
		}

		public async Task<List<PostDetailModel>> GetPostBySenderId(Guid senderId)
		{
			var posts = await _postRepository.GetPostsBySenderIdWithDetailsAsync(senderId);
			if (posts == null || !posts.Any())
			{
				return new List<PostDetailModel>();
			}
			var allProductValues = posts
				.Where(p => p.Product != null && p.Product.ProductValues != null)
				.SelectMany(p => p.Product.ProductValues)
				.ToList();

			if (!allProductValues.Any())
			{
				var emptyJsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
				return posts.Select(p => MapToPostDetailModel(
					p,
					new Dictionary<Guid, string>(),
					new Dictionary<Guid, string>(),
					emptyJsonOptions)
				).ToList();
			}
			var attrIds = allProductValues.Select(pv => pv.AttributeId).Distinct().ToList();
			var optionIds = allProductValues
				.Where(pv => pv.AttributeOptionId.HasValue)
				.Select(pv => pv.AttributeOptionId.Value)
				.Distinct()
				.ToList();
			var attributes = await _attributeRepository.GetsAsync(a => attrIds.Contains(a.AttributeId));
			var optionsList = await _attributeOptionRepository.GetsAsync(o => optionIds.Contains(o.OptionId));

			var attrDict = attributes.ToDictionary(k => k.AttributeId, v => v.Name);
			var optionDict = optionsList.ToDictionary(k => k.OptionId, v => v.OptionName);
			var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			return posts.Select(post => MapToPostDetailModel(post, attrDict, optionDict, jsonOptions)).ToList();
		}

		private PostSummaryModel MapToPostSummaryModel(Post post)
		{
			if (post == null) throw new AppException("Post không tồn tại", 404);
			var senderName = post.Sender?.Name ?? "Không rõ";
			if (post.Product == null) throw new AppException("Product của Post không tồn tại", 404);
			
			string finalCategoryName = "Không rõ";
			var directCategory = post.Product.Category;

			if (directCategory != null)
			{
				if (directCategory.ParentCategory != null)
				{
					finalCategoryName = directCategory.ParentCategory.Name;
				}
				else
				{
					finalCategoryName = directCategory.Name;
				}
			}

			string thumbnailUrl = null;
			if (post.Product.ProductImages != null && post.Product.ProductImages.Any())
			{
				thumbnailUrl = post.Product.ProductImages.FirstOrDefault()?.ImageUrl;
			}

			return new PostSummaryModel
			{
				Id = post.PostId,
				Category = finalCategoryName,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PostStatus>(post.Status),
				Date = post.Date,
				Address = post.Address,
				SenderName = senderName,
				ThumbnailUrl = thumbnailUrl,
				EstimatePoint = post.EstimatePoint
			};
		}

		private PostDetailModel MapToPostDetailModel(Post post, Dictionary<Guid, string> attrDict, Dictionary<Guid, string> optionDict, JsonSerializerOptions options)
		{
			if (post == null) throw new AppException("Post không tồn tại", 404);
			var userResponse = new UserResponse();
			if (post.Sender != null)
			{
				userResponse = new UserResponse
				{
					UserId = post.Sender.UserId,
					Avatar = post.Sender.Avatar,
					Email = post.Sender.Email,
					Name = post.Sender.Name,
					Phone = post.Sender.Phone,
					Role = post.Sender.Role,
					SmallCollectionPointId = post.Sender.SmallCollectionPointId?.ToString()
				};
			}
			var productDetail = new ProductDetailModel();
			string categoryName = "Không rõ";
			string parentCategoryName = "Không rõ";
			List<string> imageUrls = new List<string>();
			List<LabelModel> aggregatedLabels = new List<LabelModel>();

			if (post.Product != null)
			{
				if (post.Product.Category != null)
				{
					categoryName = post.Product.Category.Name;
					parentCategoryName = post.Product.Category.ParentCategory?.Name ?? "Không rõ";
				}
				productDetail.ProductId = post.Product.ProductId;
				productDetail.Description = post.Product.Description;
				productDetail.BrandId = post.Product.BrandId;
				productDetail.BrandName = post.Product.Brand?.Name ?? "Không rõ";
				if (post.Product.ProductValues != null)
				{
					productDetail.Attributes = post.Product.ProductValues.Select(pv => new ProductValueDetailModel
					{
						AttributeId = pv.AttributeId.Value,
						AttributeName = attrDict.ContainsKey(pv.AttributeId.Value) ? attrDict[pv.AttributeId.Value] : "Unknown",
						OptionId = pv.AttributeOptionId,
						OptionName = (pv.AttributeOptionId.HasValue && optionDict.ContainsKey(pv.AttributeOptionId.Value))
									 ? optionDict[pv.AttributeOptionId.Value] : null,
						Value = pv.Value.ToString()
					}).ToList();
				}

				if (post.Product.ProductImages != null)
				{
					imageUrls = post.Product.ProductImages.Select(x => x.ImageUrl).ToList();

					var allLabels = new List<LabelModel>();
					foreach (var img in post.Product.ProductImages)
					{
						if (!string.IsNullOrEmpty(img.AiDetectedLabelsJson))
						{
							try
							{
								var labels = JsonSerializer.Deserialize<List<LabelModel>>(img.AiDetectedLabelsJson, options);
								if (labels != null) allLabels.AddRange(labels);
							}
							catch { }
						}
					}
					aggregatedLabels = allLabels.GroupBy(l => l.Tag)
						.Select(g => new LabelModel { Tag = g.Key, Confidence = g.Max(x => x.Confidence), Status = g.First().Status })
						.OrderByDescending(x => x.Confidence).Take(5).ToList();
				}
			}

			List<DailyTimeSlots> schedule = null;
			if (!string.IsNullOrEmpty(post.ScheduleJson))
			{
				try { schedule = JsonSerializer.Deserialize<List<DailyTimeSlots>>(post.ScheduleJson, options); }
				catch { }
			}
			return new PostDetailModel
			{
				Id = post.PostId,
				ParentCategory = parentCategoryName,
				SubCategory = categoryName,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PostStatus>(post.Status),
				RejectMessage = post.RejectMessage,
				Date = post.Date,
				Address = post.Address,
				Sender = userResponse,
				Schedule = schedule,
				PostNote = post.Description,
				Product = productDetail,
				CheckMessage = post.CheckMessage,
				EstimatePoint = post.EstimatePoint,
				ImageUrls = imageUrls,
				AggregatedAiLabels = aggregatedLabels
			};
		}
		public async Task<bool> ApprovePost(List<Guid> postIds)
		{
			var posts = await _unitOfWork.Posts.GetsAsync(p => postIds.Contains(p.PostId));

			if (posts == null || !posts.Any())
			{
				throw new AppException("Không tìm thấy bài viết nào hợp lệ", 404);
			}

			foreach (var post in posts)
			{
				if (post.Status == PostStatus.DA_DUYET.ToString()) continue;

				post.Status = PostStatus.DA_DUYET.ToString();
				_unitOfWork.Posts.Update(post);

				if (post.ProductId != Guid.Empty && post.ProductId != null)
				{
					var product = await _unitOfWork.Products.GetByIdAsync(post.ProductId);

					if (product != null)
					{
						product.Status = ProductStatus.CHO_PHAN_KHO.ToString();
						_unitOfWork.Products.Update(product);

						var history = new ProductStatusHistory
						{
							ProductId = post.ProductId, 
							ChangedAt = DateTime.UtcNow,
							Status = ProductStatus.CHO_PHAN_KHO.ToString(),
							StatusDescription = "Yêu cầu được duyệt và chờ phân kho"
                        };

						await _unitOfWork.ProductStatusHistory.AddAsync(history);
					}
				}
			}

			 await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<bool> RejectPost(List<Guid> postIds, string rejectMessage)
		{
			var checkBadWord = await _profanityChecker.ContainsProfanityAsync(rejectMessage);
			if (checkBadWord) throw new AppException("Lý do từ chối chứa từ ngữ không phù hợp.", 400);
			

			var posts = await _unitOfWork.Posts.GetsAsync(p => postIds.Contains(p.PostId));

			if (posts == null || !posts.Any()) throw new AppException("Không tìm thấy bài viết nào hợp lệ.", 404);
			

			foreach (var post in posts)
			{
				if (post.Status == PostStatus.DA_TU_CHOI.ToString()) continue;

				post.Status = PostStatus.DA_TU_CHOI.ToString();
				post.RejectMessage = rejectMessage;
				_unitOfWork.Posts.Update(post);

				if (post.ProductId != null && post.ProductId != Guid.Empty)
				{
					var product = await _unitOfWork.Products.GetByIdAsync(post.ProductId);

					if (product != null)
					{
						product.Status = ProductStatus.DA_TU_CHOI.ToString();
						_unitOfWork.Products.Update(product);

						var history = new ProductStatusHistory
						{
							ProductId = post.ProductId,
							ChangedAt = DateTime.UtcNow,
							Status = ProductStatus.DA_TU_CHOI.ToString(),
							StatusDescription = $"Bài đăng bị từ chối. Lý do: {rejectMessage}"
						};
						await _unitOfWork.ProductStatusHistory.AddAsync(history);
					}
				}
			}

			await _unitOfWork.SaveAsync();

			return true;
		}

		public async Task<PagedResultModel<PostSummaryModel>> GetPagedPostsAsync(PostSearchQueryModel model)
		{
			if (model == null)
			{
				throw new AppException("Invalid search model.", 400);
			}
			string? statusEnum = null;
			if (model.Status != null)
			{
				statusEnum = StatusEnumHelper.GetValueFromDescription<PostStatus>(model.Status).ToString();
			}
			var (posts, totalItems) = await _postRepository.GetPagedPostsAsync(
				status: statusEnum,
				search: model.Search,
				order: model.Order,
				page: model.Page,
				limit: model.Limit
			);

			var summaryList = posts
				.Select(p => MapToPostSummaryModel(p))
				.ToList();
			return new PagedResultModel<PostSummaryModel>(
				summaryList,
				model.Page,
				model.Limit,
				totalItems
			);
		}
	}
}