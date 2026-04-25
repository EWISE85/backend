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
using System.Text.Json.Serialization;
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
		private readonly IUserAddressRepository _userAddressRepository;
		private readonly ICategoryAttributeRepsitory _categoryAttributeRepsitory;
		private readonly IBrandCategoryRepository _brandCategoryRepository;
		private readonly INotificationService _notificationService;
		private readonly IRedisCacheService _redisCacheService;
		public PostService(IProfanityChecker profanityChecker, IProductService productService, IImageRecognitionService imageRecognitionService, IProductImageRepository productImageRepository, IProductRepository productRepository, IProductValuesRepository productValuesRepository, IUnitOfWork unitOfWork, IPostRepository postRepository, IProductStatusHistoryRepository productStatusHistoryRepository, ICategoryRepository categoryRepository, IAttributeRepository attributeRepository, IAttributeOptionRepository attributeOptionRepository, IMapboxDistanceCacheService distanceCache, IUserAddressRepository userAddressRepository, ICategoryAttributeRepsitory categoryAttributeRepsitory, IBrandCategoryRepository brandCategoryRepository, INotificationService notificationService, IRedisCacheService redisCacheService )
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
			_userAddressRepository = userAddressRepository;
			_categoryAttributeRepsitory = categoryAttributeRepsitory;
			_brandCategoryRepository = brandCategoryRepository;
			_notificationService = notificationService;
			_redisCacheService = redisCacheService;
		}

		//public async Task<bool> AddPost(CreatePostModel createPostRequest)
		//{
		//	if (createPostRequest.Product == null) throw new AppException("Product đang trống", 400);

		//	var userLocation = await _userAddressRepository.GetAsync(
		//		a => a.UserId == createPostRequest.SenderId && a.Address == createPostRequest.Address);

		//	if (userLocation == null || !userLocation.Iat.HasValue || !userLocation.Ing.HasValue)
		//	{
		//		throw new AppException("Địa chỉ không hợp lệ hoặc chưa được định vị trên bản đồ.", 400);
		//	}

		//	bool isServiceable = await CheckIfLocationAndCategoryAreServiceable(
		//		createPostRequest.Product.SubCategoryId,
		//		userLocation.Iat.Value,
		//		userLocation.Ing.Value);

		//	if (!isServiceable)
		//	{
		//		throw new AppException("Rất tiếc, loại hàng này hiện chưa được hỗ trợ thu gom tại khu vực của bạn.", 400);
		//	}

		//	DateTime transactionTimeUtc = DateTime.UtcNow;
		//	try
		//	{
		//		var validationRules = await _categoryAttributeRepsitory.GetsAsync(
		//								x => x.CategoryId == createPostRequest.Product.SubCategoryId,
		//								includeProperties: "Attribute");

		//		string currentStatus = PostStatus.CHO_DUYET.ToString();
		//		string currentProductStatus = ProductStatus.CHO_DUYET.ToString();
		//		string statusDescription = "Yêu cầu đã được gửi";
		//		Guid newProductId = Guid.NewGuid();

		//		var brandCategoryPoint = await _brandCategoryRepository.GetAsync(bc => bc.BrandId == createPostRequest.Product.BrandId && bc.CategoryId == createPostRequest.Product.SubCategoryId);
		//		var basePoint = brandCategoryPoint != null ? brandCategoryPoint.Points : 50;

		//		var newPost = new Post
		//		{
		//			PostId = Guid.NewGuid(),
		//			SenderId = createPostRequest.SenderId,
		//			Date = DateTime.UtcNow,
		//			Description = createPostRequest.Description,
		//			Address = createPostRequest.Address,
		//			ScheduleJson = JsonSerializer.Serialize(createPostRequest.CollectionSchedule),
		//			Status = currentStatus,
		//			EstimatePoint = basePoint,
		//			CheckMessage = new List<string>()
		//		};

		//		var newProduct = new Products
		//		{
		//			ProductId = newProductId,
		//			CategoryId = createPostRequest.Product.SubCategoryId,
		//			BrandId = createPostRequest.Product.BrandId,
		//			Description = createPostRequest.Description,
		//			CreateAt = DateOnly.FromDateTime(transactionTimeUtc),
		//			UserId = createPostRequest.SenderId,
		//			isChecked = false,
		//			PostId = newPost.PostId,
		//			Status = currentProductStatus
		//		};

		//		if (createPostRequest.Product.Attributes != null)
		//		{
		//			foreach (var attr in createPostRequest.Product.Attributes)
		//			{
		//				var rule = validationRules.FirstOrDefault(x => x.AttributeId == attr.AttributeId);
		//				if (rule == null)
		//				{
		//					throw new AppException($"Thuộc tính với ID '{attr.AttributeId}' không hợp lệ cho danh mục này.", 400);
		//				}
		//				var attributeName = rule?.Attribute?.Name ?? "Unknown Attribute";
		//				if (attr.OptionId == null && attr.Value.HasValue && rule != null)
		//				{
		//					if (rule.MinValue.HasValue && attr.Value.Value < rule.MinValue.Value)
		//					{
		//						throw new AppException($"Giá trị của '{rule.Attribute.Name}' quá nhỏ. Tối thiểu phải là {rule.MinValue} {rule.Unit}.", 400);
		//					}
		//				}
		//				if (attributeName == "Trọng lượng (kg)")
		//				{
		//					if (attr.OptionId.HasValue)
		//					{
		//						var option = await _unitOfWork.AttributeOptions.GetAsync(o => o.OptionId == attr.OptionId.Value);
		//						if (option == null)
		//						{
		//							throw new AppException($"OptionId '{attr.OptionId.Value}' không tồn tại.", 400);
		//						}
		//						if (option.EstimateWeight < rule.MinValue.Value)
		//						{
		//							throw new AppException($"Giá trị của '{rule.Attribute.Name}' quá nhỏ. Tối thiểu phải là {rule.MinValue} {rule.Unit}.", 400);
		//						}
		//					}
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

		//			var aiTags = category?.AiRecognitionTags;
		//			bool allImagesMatch = true;

		//			foreach (var imgUrl in createPostRequest.Images)
		//			{
		//				var aiResult = await _imageRecognitionService.AnalyzeImageCategoryAsync(imgUrl, aiTags);
		//				if (aiResult == null || !aiResult.IsMatch) allImagesMatch = false;

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
		//				newPost.Status = PostStatus.DA_DUYET.ToString();
		//				newProduct.Status = ProductStatus.CHO_PHAN_KHO.ToString();
		//				statusDescription = "Yêu cầu được duyệt tự động, chờ phân về kho tương ứng";
		//			}
		//		}

		//		var history = new ProductStatusHistory
		//		{
		//			ProductId = newProductId,
		//			ChangedAt = DateTime.UtcNow,
		//			Status = newProduct.Status,
		//			StatusDescription = statusDescription
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

		public async Task<bool> AddPost(CreatePostModel createPostRequest)
		{
			if (createPostRequest.Product == null) throw new AppException("Product đang trống", 400);

			var userLocation = await _userAddressRepository.GetAsync(
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
				var validationRules = await _categoryAttributeRepsitory.GetsAsync(
										x => x.CategoryId == createPostRequest.Product.SubCategoryId,
										includeProperties: "Attribute");

				string currentStatus = PostStatus.CHO_DUYET.ToString();
				string currentProductStatus = ProductStatus.CHO_DUYET.ToString();
				string statusDescription = "Yêu cầu đã được gửi";
				Guid newProductId = Guid.NewGuid();

				var brandCategoryPoint = await _brandCategoryRepository.GetAsync(bc => bc.BrandId == createPostRequest.Product.BrandId && bc.CategoryId == createPostRequest.Product.SubCategoryId);
				var basePoint = brandCategoryPoint != null ? brandCategoryPoint.Points : 50;

				var newPost = new Post
				{
					PostId = Guid.NewGuid(),
					SenderId = createPostRequest.SenderId,
					Date = DateTime.UtcNow,
					Description = createPostRequest.Description,
					Address = createPostRequest.Address,
					ScheduleJson = JsonSerializer.Serialize(createPostRequest.CollectionSchedule),
					Status = currentStatus,
					EstimatePoint = basePoint,
					CheckMessage = new List<string>()
				};

				var newProduct = new Products
				{
					ProductId = newProductId,
					CategoryId = createPostRequest.Product.SubCategoryId,
					BrandId = createPostRequest.Product.BrandId,
					Description = createPostRequest.Description,
					CreateAt = DateOnly.FromDateTime(transactionTimeUtc),
					UserId = createPostRequest.SenderId,
					isChecked = false,
					PostId = newPost.PostId,
					Status = currentProductStatus
				};

				var productValuesList = new List<ProductValues>();
				var productImagesList = new List<Image>();

				if (createPostRequest.Product.Attributes != null)
				{
					foreach (var attr in createPostRequest.Product.Attributes)
					{
						var rule = validationRules.FirstOrDefault(x => x.AttributeId == attr.AttributeId);
						if (rule == null)
						{
							throw new AppException($"Thuộc tính với ID '{attr.AttributeId}' không hợp lệ cho danh mục này.", 400);
						}
						var attributeName = rule?.Attribute?.Name ?? "Unknown Attribute";
						if (attr.OptionId == null && attr.Value.HasValue && rule != null)
						{
							if (rule.MinValue.HasValue && attr.Value.Value < rule.MinValue.Value)
							{
								throw new AppException($"Giá trị của '{rule.Attribute.Name}' quá nhỏ. Tối thiểu phải là {rule.MinValue} {rule.Unit}.", 400);
							}
						}
						if (attributeName == "Trọng lượng (kg)")
						{
							if (attr.OptionId.HasValue)
							{
								var option = await _unitOfWork.AttributeOptions.GetAsync(o => o.OptionId == attr.OptionId.Value);
								if (option == null)
								{
									throw new AppException($"OptionId '{attr.OptionId.Value}' không tồn tại.", 400);
								}
								if (option.EstimateWeight < rule.MinValue.Value)
								{
									throw new AppException($"Giá trị của '{rule.Attribute.Name}' quá nhỏ. Tối thiểu phải là {rule.MinValue} {rule.Unit}.", 400);
								}
							}
						}
						var newProductValue = new ProductValues
						{
							ProductValuesId = Guid.NewGuid(),
							ProductId = newProductId,
							AttributeId = attr.AttributeId,
							AttributeOptionId = attr.OptionId,
							Value = attr.Value,

						};
						productValuesList.Add(newProductValue);
					}
				}

				if (createPostRequest.Images != null && createPostRequest.Images.Any())
				{
					var category = await _categoryRepository.GetByIdAsync(createPostRequest.Product.SubCategoryId);

					var aiTags = category?.AiRecognitionTags;
					bool allImagesMatch = true;

					foreach (var imgUrl in createPostRequest.Images)
					{
						var aiResult = await _imageRecognitionService.AnalyzeImageCategoryAsync(imgUrl, aiTags);
						if (aiResult == null || !aiResult.IsMatch) allImagesMatch = false;

						var productImg = new Image
						{
							Id = Guid.NewGuid(),
							ProductId = null,
							ImageUrl = imgUrl,
							PostId = newPost.PostId,
							AiDetectedLabelsJson = aiResult?.DetectedTagsJson ?? "[]"
						};
						productImagesList.Add(productImg);
					}

					if (allImagesMatch)
					{
						newPost.Status = PostStatus.DA_DUYET.ToString();
						newProduct.Status = ProductStatus.CHO_PHAN_KHO.ToString();
						statusDescription = "Yêu cầu được duyệt tự động, chờ phân về kho tương ứng";
						foreach (var img in productImagesList)
						{
							img.ProductId = newProductId;
						}
					}
				}

				var history = new ProductStatusHistory
				{
					ProductId = newProductId,
					ChangedAt = DateTime.UtcNow,
					Status = newProduct.Status,
					StatusDescription = statusDescription
				};

				await _unitOfWork.Posts.AddAsync(newPost);
				foreach (var pi in productImagesList)
				{
					await _unitOfWork.Images.AddAsync(pi);
				}
				if (newPost.Status == PostStatus.DA_DUYET.ToString())
				{
					await _unitOfWork.Products.AddAsync(newProduct);
					foreach (var pv in productValuesList) await _unitOfWork.ProductValues.AddAsync(pv);
					//foreach (var pi in productImagesList) await _unitOfWork.ProductImages.AddAsync(pi);
					await _unitOfWork.ProductStatusHistory.AddAsync(history);
				}
				else
				{
					var childCategory = await _categoryRepository.GetAsync(
	c => c.CategoryId == createPostRequest.Product.SubCategoryId,
	includeProperties: "ParentCategory"
);

					// Giả sử category có thuộc tính ParentCategory, nếu không bạn query riêng
					string parentCategoryName = childCategory?.ParentCategory?.Name ?? childCategory?.Name ?? "Không rõ";

					// Nếu bạn có _brandRepository thì dùng, không thì lấy qua _unitOfWork
					var brand = await _unitOfWork.Brands.GetByIdAsync(createPostRequest.Product.BrandId);
					var draftData = new
					{
						Product = newProduct,
						ProductValues = productValuesList,
						//ProductImages = productImagesList,
						History = history,
						CategoryName = parentCategoryName,
						ChildCategoryName = childCategory?.Name ?? "Không rõ",
						BrandName = brand?.Name ?? "Không rõ"
					};

					var redisKey = $"ewise:draft_product:{newPost.PostId}";

					var jsonOptions = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };
					var draftJson = JsonSerializer.Serialize(draftData, jsonOptions);

					await _redisCacheService.SetStringAsync(redisKey, draftJson, TimeSpan.FromDays(7));
					var productId = draftData.Product.ProductId; 
					var postId = newPost.PostId;

					await _redisCacheService.SetStringAsync(
						$"ewise:product_map:{productId}",
						postId.ToString(),
						TimeSpan.FromDays(30) 
					);
				}

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

            var allCompanies = await _unitOfWork.Companies.GetAllAsync(includeProperties: "CollectionUnits,CompanyRecyclingCategories");
            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();
            var recyclingCompanies = allCompanies.Where(c => c.CompanyType == CompanyType.CTY_TAI_CHE.ToString()).ToList();

            var validCandidates = new List<CollectionUnit>();

            foreach (var comp in allCompanies.Where(c => c.CompanyType == CompanyType.CTY_TAI_CHE.ToString()))
            {
                foreach (var sp in comp.CollectionUnits.Where(s => s.Status == CompanyStatus.DANG_HOAT_DONG.ToString()))
                {
                    var rComp = recyclingCompanies.FirstOrDefault(c => c.CompanyId == sp.CompanyId);
                    bool canHandle = rComp?.CompanyRecyclingCategories.Any(crc => crc.CategoryId == rootCateId) ?? false;

                    if (!canHandle) continue;

                    double hvDist = GeoHelper.DistanceKm(sp.Latitude, sp.Longitude, userLat, userLng);
                    double radius = GetConfigValue(allConfigs, null, sp.CollectionUnitId, SystemConfigKey.RADIUS_KM, 10);

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
                double maxRoad = GetConfigValue(allConfigs, null, point.CollectionUnitId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, 15);
                double hvDist = GeoHelper.DistanceKm(point.Latitude, point.Longitude, userLat, userLng);

                if (roadDistances.TryGetValue(point.CollectionUnitId, out double roadKm))
                {
                    if (roadKm <= maxRoad) return true;
                }
                else
                {

                    if (hvDist <= maxRoad)
                    {
                        Console.WriteLine($"[AddPost Fallback] Mapbox Error. Approved by Haversine: {hvDist}km for Point: {point.CollectionUnitId}");
                        return true;
                    }
                }
            }

            return false;
        }

        private double GetConfigValue(IEnumerable<SystemConfig> configs, string? companyId, string? pointId, SystemConfigKey key, double defaultValue)
        {
            var cfg = configs.FirstOrDefault(c => c.Key == key.ToString() && c.CompanyId == companyId && c.CollectionUnitId == pointId)
                   ?? configs.FirstOrDefault(c => c.Key == key.ToString() && c.CompanyId == companyId && c.CollectionUnitId == null)
                   ?? configs.FirstOrDefault(c => c.Key == key.ToString() && c.CompanyId == null && c.CollectionUnitId == null);

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

		//public async Task<PostDetailModel> GetById(Guid id)
		//{
		//	var post = await _postRepository.GetPostWithDetailsAsync(id);
		//	if (post == null) return null;
		//	var productValues = post.Product?.ProductValues ?? new List<ProductValues>();

		//	var attrIds = productValues.Select(pv => pv.AttributeId).Distinct().ToList();
		//	var optionIds = productValues.Where(pv => pv.AttributeOptionId.HasValue)
		//								 .Select(pv => pv.AttributeOptionId.Value)
		//								 .Distinct().ToList();
		//	var attributes = await _attributeRepository.GetsAsync(a => attrIds.Contains(a.AttributeId));
		//	var optionsList = await _attributeOptionRepository.GetsAsync(o => optionIds.Contains(o.OptionId));
		//	var attrDict = attributes.ToDictionary(k => k.AttributeId, v => v.Name);
		//	var optionDict = optionsList.ToDictionary(k => k.OptionId, v => v.OptionName);
		//	var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		//	return MapToPostDetailModel(post, attrDict, optionDict, jsonOptions);
		//}
		public async Task<PostDetailModel> GetById(Guid id)
		{
			var post = await _postRepository.GetPostWithDetailsAsync(id);
			if (post == null) return null;

			var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			ProductDraftModel draftData = null;

			// 1. TÌM DỮ LIỆU SẢN PHẨM TỪ REDIS HOẶC CHECK_MESSAGE (Dành cho bài chưa duyệt / từ chối)
			if (post.Status == PostStatus.CHO_DUYET.ToString())
			{
				var redisKey = $"ewise:draft_product:{post.PostId}";
				var draftJson = await _redisCacheService.GetStringAsync(redisKey);
				if (!string.IsNullOrEmpty(draftJson))
				{
					draftData = JsonSerializer.Deserialize<ProductDraftModel>(draftJson, jsonOptions);
				}
			}
			else if (post.Status == PostStatus.DA_TU_CHOI.ToString() && post.CheckMessage != null && post.CheckMessage.Any(x => x.StartsWith("[REJECTED_PRODUCT_DATA]")))
			{
				var draftString = post.CheckMessage.First(x => x.StartsWith("[REJECTED_PRODUCT_DATA]")).Split('|')[1];
				draftData = JsonSerializer.Deserialize<ProductDraftModel>(draftString, jsonOptions);
			}

			// 2. LẤY DANH SÁCH THUỘC TÍNH (Từ Nháp hoặc Từ SQL)
			var productValues = new List<ProductValues>();
			if (draftData != null && draftData.ProductValues != null)
			{
				productValues = draftData.ProductValues;
			}
			else if (post.Product != null && post.Product.ProductValues != null)
			{
				productValues = post.Product.ProductValues.ToList();
			}

			var attrIds = productValues.Where(pv => pv.AttributeId.HasValue)
									   .Select(pv => pv.AttributeId.Value).Distinct().ToList();
			var optionIds = productValues.Where(pv => pv.AttributeOptionId.HasValue)
										 .Select(pv => pv.AttributeOptionId.Value).Distinct().ToList();

			var attributes = await _attributeRepository.GetsAsync(a => attrIds.Contains(a.AttributeId));
			var optionsList = await _attributeOptionRepository.GetsAsync(o => optionIds.Contains(o.OptionId));

			var attrDict = attributes.ToDictionary(k => k.AttributeId, v => v.Name);
			var optionDict = optionsList.ToDictionary(k => k.OptionId, v => v.OptionName);

			// 3. MAP DỮ LIỆU (Truyền thêm draftData vào hàm Mapper)
			return MapToPostDetailModel(post, draftData, attrDict, optionDict, jsonOptions);
		}

		private PostDetailModel MapToPostDetailModel(Post post, ProductDraftModel draft, Dictionary<Guid, string> attrDict, Dictionary<Guid, string> optionDict, JsonSerializerOptions options)
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
					SmallCollectionPointId = post.Sender.CollectionUnitId?.ToString()
				};
			}

			var productDetail = new ProductDetailModel();
			string categoryName = "Không rõ";
			string parentCategoryName = "Không rõ";
			List<string> imageUrls = new List<string>();
			List<LabelModel> aggregatedLabels = new List<LabelModel>();

			// --- MAP DỮ LIỆU SẢN PHẨM ---
			if (draft != null)
			{
				// LUỒNG 1: Map từ dữ liệu nháp (Redis / CheckMessage)
				categoryName = draft.ChildCategoryName ?? "Không rõ";
				parentCategoryName = draft.CategoryName ?? "Không rõ";

				if (draft.Product != null)
				{
					productDetail.ProductId = draft.Product.ProductId;
					productDetail.Description = draft.Product.Description;
					productDetail.BrandId = draft.Product.BrandId;
					productDetail.BrandName = draft.BrandName ?? "Không rõ";
				}

				if (draft.ProductValues != null)
				{
					productDetail.Attributes = draft.ProductValues.Where(pv => pv.AttributeId.HasValue).Select(pv => new ProductValueDetailModel
					{
						AttributeId = pv.AttributeId.Value,
						AttributeName = attrDict.ContainsKey(pv.AttributeId.Value) ? attrDict[pv.AttributeId.Value] : "Unknown",
						OptionId = pv.AttributeOptionId,
						OptionName = (pv.AttributeOptionId.HasValue && optionDict.ContainsKey(pv.AttributeOptionId.Value)) ? optionDict[pv.AttributeOptionId.Value] : null,
						Value = pv.Value?.ToString()
					}).ToList();
				}

				//if (draft.ProductImages != null)
				//{
				//	imageUrls = draft.ProductImages.Select(x => x.ImageUrl).ToList();
				//	var allLabels = new List<LabelModel>();
				//	foreach (var img in draft.ProductImages)
				//	{
				//		if (!string.IsNullOrEmpty(img.AiDetectedLabelsJson))
				//		{
				//			try { allLabels.AddRange(JsonSerializer.Deserialize<List<LabelModel>>(img.AiDetectedLabelsJson, options)); } catch { }
				//		}
				//	}
				//	aggregatedLabels = allLabels.GroupBy(l => l.Tag)
				//		.Select(g => new LabelModel { Tag = g.Key, Confidence = g.Max(x => x.Confidence), Status = g.First().Status })
				//		.OrderByDescending(x => x.Confidence).Take(5).ToList();
				//}
			}
			else if (post.Product != null)
			{
				// LUỒNG 2: Map từ SQL Entity chuẩn (Giữ nguyên logic cũ của bạn)
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
					productDetail.Attributes = post.Product.ProductValues.Where(pv => pv.AttributeId.HasValue).Select(pv => new ProductValueDetailModel
					{
						AttributeId = pv.AttributeId.Value,
						AttributeName = attrDict.ContainsKey(pv.AttributeId.Value) ? attrDict[pv.AttributeId.Value] : "Unknown",
						OptionId = pv.AttributeOptionId,
						OptionName = (pv.AttributeOptionId.HasValue && optionDict.ContainsKey(pv.AttributeOptionId.Value)) ? optionDict[pv.AttributeOptionId.Value] : null,
						Value = pv.Value?.ToString()
					}).ToList();
				}

				//if (post.Product.Images != null)
				//{
				//	imageUrls = post.Product.Images.Select(x => x.ImageUrl).ToList();
				//	var allLabels = new List<LabelModel>();
				//	foreach (var img in post.Product.Images)
				//	{
				//		if (!string.IsNullOrEmpty(img.AiDetectedLabelsJson))
				//		{
				//			try { allLabels.AddRange(JsonSerializer.Deserialize<List<LabelModel>>(img.AiDetectedLabelsJson, options)); } catch { }
				//		}
				//	}
				//	aggregatedLabels = allLabels.GroupBy(l => l.Tag)
				//		.Select(g => new LabelModel { Tag = g.Key, Confidence = g.Max(x => x.Confidence), Status = g.First().Status })
				//		.OrderByDescending(x => x.Confidence).Take(5).ToList();
				//}
			}
			if (post.Images != null && post.Images.Any())
			{
				imageUrls = post.Images.Select(x => x.ImageUrl).ToList();
				var allLabels = new List<LabelModel>();
				foreach (var img in post.Images)
				{
					if (!string.IsNullOrEmpty(img.AiDetectedLabelsJson))
					{
						try { allLabels.AddRange(JsonSerializer.Deserialize<List<LabelModel>>(img.AiDetectedLabelsJson, options)); } catch { }
					}
				}
				aggregatedLabels = allLabels.GroupBy(l => l.Tag)
					.Select(g => new LabelModel { Tag = g.Key, Confidence = g.Max(x => x.Confidence), Status = g.First().Status })
					.OrderByDescending(x => x.Confidence).Take(5).ToList();
			}
			List<DailyTimeSlots> schedule = null;
			if (!string.IsNullOrEmpty(post.ScheduleJson))
			{
				try { schedule = JsonSerializer.Deserialize<List<DailyTimeSlots>>(post.ScheduleJson, options); }
				catch { }
			}

			// LỌC CHECK MESSAGE: Ẩn chuỗi JSON của cái Trick đi để Frontend không bị lỗi hiển thị
			var cleanCheckMessage = post.CheckMessage?.Where(x => !x.StartsWith("[REJECTED_PRODUCT_DATA]")).ToList();

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
				CheckMessage = cleanCheckMessage, // Trả về list đã làm sạch
				EstimatePoint = post.EstimatePoint,
				ImageUrls = imageUrls,
				AggregatedAiLabels = aggregatedLabels
			};
		}
		//public async Task<List<PostDetailModel>> GetPostBySenderId(Guid senderId)
		//{
		//	var posts = await _postRepository.GetPostsBySenderIdWithDetailsAsync(senderId);
		//	if (posts == null || !posts.Any())
		//	{
		//		return new List<PostDetailModel>();
		//	}
		//	var allProductValues = posts
		//		.Where(p => p.Product != null && p.Product.ProductValues != null)
		//		.SelectMany(p => p.Product.ProductValues)
		//		.ToList();

		//	if (!allProductValues.Any())
		//	{
		//		var emptyJsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		//		return posts.Select(p => MapToPostDetailModel(
		//			p,
		//			new Dictionary<Guid, string>(),
		//			new Dictionary<Guid, string>(),
		//			emptyJsonOptions)
		//		).ToList();
		//	}
		//	var attrIds = allProductValues.Select(pv => pv.AttributeId).Distinct().ToList();
		//	var optionIds = allProductValues
		//		.Where(pv => pv.AttributeOptionId.HasValue)
		//		.Select(pv => pv.AttributeOptionId.Value)
		//		.Distinct()
		//		.ToList();
		//	var attributes = await _attributeRepository.GetsAsync(a => attrIds.Contains(a.AttributeId));
		//	var optionsList = await _attributeOptionRepository.GetsAsync(o => optionIds.Contains(o.OptionId));

		//	var attrDict = attributes.ToDictionary(k => k.AttributeId, v => v.Name);
		//	var optionDict = optionsList.ToDictionary(k => k.OptionId, v => v.OptionName);
		//	var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		//	return posts.Select(post => MapToPostDetailModel(post, attrDict, optionDict, jsonOptions)).ToList();
		//}
		public async Task<List<PostDetailModel>> GetPostBySenderId(Guid senderId)
		{
			var posts = await _postRepository.GetPostsBySenderIdWithDetailsAsync(senderId);
			if (posts == null || !posts.Any())
			{
				return new List<PostDetailModel>();
			}

			var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			// Dictionary để lưu trữ tạm các Draft giải nén được, gắn theo PostId
			var draftDict = new Dictionary<Guid, ProductDraftModel>();

			// List để gom TẤT CẢ ProductValues từ SQL và Redis lại với nhau
			var allProductValues = new List<ProductValues>();

			// 1. DUYỆT QUA TỪNG POST ĐỂ LẤY DỮ LIỆU SẢN PHẨM (SQL HOẶC DRAFT)
			foreach (var post in posts)
			{
				ProductDraftModel draftData = null;

				if (post.Status == PostStatus.CHO_DUYET.ToString())
				{
					var redisKey = $"ewise:draft_product:{post.PostId}";
					var draftJson = await _redisCacheService.GetStringAsync(redisKey);
					if (!string.IsNullOrEmpty(draftJson))
					{
						draftData = JsonSerializer.Deserialize<ProductDraftModel>(draftJson, jsonOptions);
					}
				}
				else if (post.Status == PostStatus.DA_TU_CHOI.ToString() && post.CheckMessage != null && post.CheckMessage.Any(x => x.StartsWith("[REJECTED_PRODUCT_DATA]")))
				{
					var draftString = post.CheckMessage.First(x => x.StartsWith("[REJECTED_PRODUCT_DATA]")).Split('|')[1];
					draftData = JsonSerializer.Deserialize<ProductDraftModel>(draftString, jsonOptions);
				}

				// Nếu có dữ liệu Nháp (Draft), gom ProductValues của Nháp
				if (draftData != null)
				{
					draftDict[post.PostId] = draftData;
					if (draftData.ProductValues != null)
					{
						allProductValues.AddRange(draftData.ProductValues);
					}
				}
				else if (post.Product != null && post.Product.ProductValues != null)
				{
					allProductValues.AddRange(post.Product.ProductValues);
				}
			}

			if (!allProductValues.Any())
			{
				return posts.Select(post =>
				{
					draftDict.TryGetValue(post.PostId, out var draft);
					return MapToPostDetailModel(post, draft, new Dictionary<Guid, string>(), new Dictionary<Guid, string>(), jsonOptions);
				}).ToList();
			}

			// 3. QUERY DATABASE 1 LẦN DUY NHẤT ĐỂ LẤY TÊN THUỘC TÍNH VÀ OPTION
			var attrIds = allProductValues
				.Where(pv => pv.AttributeId.HasValue)
				.Select(pv => pv.AttributeId.Value)
				.Distinct()
				.ToList();

			var optionIds = allProductValues
				.Where(pv => pv.AttributeOptionId.HasValue)
				.Select(pv => pv.AttributeOptionId.Value)
				.Distinct()
				.ToList();

			var attributes = await _attributeRepository.GetsAsync(a => attrIds.Contains(a.AttributeId));
			var optionsList = await _attributeOptionRepository.GetsAsync(o => optionIds.Contains(o.OptionId));

			var attrDict = attributes.ToDictionary(k => k.AttributeId, v => v.Name);
			var optionDict = optionsList.ToDictionary(k => k.OptionId, v => v.OptionName);

			// 4. MAP DỮ LIỆU TỪNG POST SANG MODEL
			var resultList = new List<PostDetailModel>();
			foreach (var post in posts)
			{
				// Lấy draft tương ứng với bài Post (nếu có)
				draftDict.TryGetValue(post.PostId, out var draft);

				// Gọi hàm MapToPostDetailModel mà chúng ta đã sửa ở Bước trước
				resultList.Add(MapToPostDetailModel(post, draft, attrDict, optionDict, jsonOptions));
			}

			return resultList;
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
			if (post.Product.Images != null && post.Product.Images.Any())
			{
				thumbnailUrl = post.Product.Images.FirstOrDefault()?.ImageUrl;
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
				EstimatePoint = post.EstimatePoint,
				BrandName = post.Product.Brand?.Name ?? "Không rõ",
				ChildCategoryName = post.Product.Category?.Name ?? "Không rõ"
			};
		}

		//private PostDetailModel MapToPostDetailModel(Post post, Dictionary<Guid, string> attrDict, Dictionary<Guid, string> optionDict, JsonSerializerOptions options)
		//{
		//	if (post == null) throw new AppException("Post không tồn tại", 404);
		//	var userResponse = new UserResponse();
		//	if (post.Sender != null)
		//	{
		//		userResponse = new UserResponse
		//		{
		//			UserId = post.Sender.UserId,
		//			Avatar = post.Sender.Avatar,
		//			Email = post.Sender.Email,
		//			Name = post.Sender.Name,
		//			Phone = post.Sender.Phone,
		//			Role = post.Sender.Role,
		//			SmallCollectionPointId = post.Sender.CollectionUnitId?.ToString()
		//		};
		//	}
		//	var productDetail = new ProductDetailModel();
		//	string categoryName = "Không rõ";
		//	string parentCategoryName = "Không rõ";
		//	List<string> imageUrls = new List<string>();
		//	List<LabelModel> aggregatedLabels = new List<LabelModel>();

		//	if (post.Product != null)
		//	{
		//		if (post.Product.Category != null)
		//		{
		//			categoryName = post.Product.Category.Name;
		//			parentCategoryName = post.Product.Category.ParentCategory?.Name ?? "Không rõ";
		//		}
		//		productDetail.ProductId = post.Product.ProductId;
		//		productDetail.Description = post.Product.Description;
		//		productDetail.BrandId = post.Product.BrandId;
		//		productDetail.BrandName = post.Product.Brand?.Name ?? "Không rõ";
		//		if (post.Product.ProductValues != null)
		//		{
		//			productDetail.Attributes = post.Product.ProductValues.Select(pv => new ProductValueDetailModel
		//			{
		//				AttributeId = pv.AttributeId.Value,
		//				AttributeName = attrDict.ContainsKey(pv.AttributeId.Value) ? attrDict[pv.AttributeId.Value] : "Unknown",
		//				OptionId = pv.AttributeOptionId,
		//				OptionName = (pv.AttributeOptionId.HasValue && optionDict.ContainsKey(pv.AttributeOptionId.Value))
		//							 ? optionDict[pv.AttributeOptionId.Value] : null,
		//				Value = pv.Value.ToString()
		//			}).ToList();
		//		}

		//		if (post.Product.ProductImages != null)
		//		{
		//			imageUrls = post.Product.ProductImages.Select(x => x.ImageUrl).ToList();

		//			var allLabels = new List<LabelModel>();
		//			foreach (var img in post.Product.ProductImages)
		//			{
		//				if (!string.IsNullOrEmpty(img.AiDetectedLabelsJson))
		//				{
		//					try
		//					{
		//						var labels = JsonSerializer.Deserialize<List<LabelModel>>(img.AiDetectedLabelsJson, options);
		//						if (labels != null) allLabels.AddRange(labels);
		//					}
		//					catch { }
		//				}
		//			}
		//			aggregatedLabels = allLabels.GroupBy(l => l.Tag)
		//				.Select(g => new LabelModel { Tag = g.Key, Confidence = g.Max(x => x.Confidence), Status = g.First().Status })
		//				.OrderByDescending(x => x.Confidence).Take(5).ToList();
		//		}
		//	}

		//	List<DailyTimeSlots> schedule = null;
		//	if (!string.IsNullOrEmpty(post.ScheduleJson))
		//	{
		//		try { schedule = JsonSerializer.Deserialize<List<DailyTimeSlots>>(post.ScheduleJson, options); }
		//		catch { }
		//	}
		//	return new PostDetailModel
		//	{
		//		Id = post.PostId,
		//		ParentCategory = parentCategoryName,
		//		SubCategory = categoryName,
		//		Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PostStatus>(post.Status),
		//		RejectMessage = post.RejectMessage,
		//		Date = post.Date,
		//		Address = post.Address,
		//		Sender = userResponse,
		//		Schedule = schedule,
		//		PostNote = post.Description,
		//		Product = productDetail,
		//		CheckMessage = post.CheckMessage,
		//		EstimatePoint = post.EstimatePoint,
		//		ImageUrls = imageUrls,
		//		AggregatedAiLabels = aggregatedLabels
		//	};
		//}
		//public async Task<bool> ApprovePost(List<Guid> postIds)
		//{
		//	var posts = await _unitOfWork.Posts.GetsAsync(p => postIds.Contains(p.PostId), includeProperties : "Product");

		//	if (posts == null || !posts.Any())
		//	{
		//		throw new AppException("Không tìm thấy bài viết nào hợp lệ", 404);
		//	}

		//	var newlyApprovedPosts = new List<Post>();

		//	foreach (var post in posts)
		//	{
		//		if (post.Status == PostStatus.DA_DUYET.ToString()) continue;

		//		post.Status = PostStatus.DA_DUYET.ToString();
		//		_unitOfWork.Posts.Update(post);

		//		if (post.Product.ProductId != Guid.Empty && post.Product.ProductId != null)
		//		{
		//			var product = await _unitOfWork.Products.GetByIdAsync(post.Product.ProductId);

		//			if (product != null)
		//			{
		//				product.Status = ProductStatus.CHO_PHAN_KHO.ToString();
		//				_unitOfWork.Products.Update(product);

		//				var history = new ProductStatusHistory
		//				{
		//					ProductId = post.Product.ProductId,
		//					ChangedAt = DateTime.UtcNow,
		//					Status = ProductStatus.CHO_PHAN_KHO.ToString(),
		//					StatusDescription = "Yêu cầu được duyệt và chờ phân kho"
		//				};

		//				await _unitOfWork.ProductStatusHistory.AddAsync(history);
		//			}
		//		}

		//		// Thêm bài viết vào danh sách cần gửi thông báo
		//		newlyApprovedPosts.Add(post);
		//	}

		//	if (newlyApprovedPosts.Any())
		//	{
		//		await _notificationService.ProcessApprovalNotificationsAsync(newlyApprovedPosts);
		//	}

		//	await _unitOfWork.SaveAsync();

		//	return true;
		//}

		public async Task<bool> ApprovePost(List<Guid> postIds)
		{
			var posts = await _unitOfWork.Posts.GetsAsync(p => postIds.Contains(p.PostId), includeProperties: "Product");

			if (posts == null || !posts.Any())
			{
				throw new AppException("Không tìm thấy bài viết nào hợp lệ", 404);
			}

			var newlyApprovedPosts = new List<Post>();
			var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			foreach (var post in posts)
			{
				// Nếu đã duyệt rồi thì bỏ qua
				if (post.Status == PostStatus.DA_DUYET.ToString()) continue;

				// 1. Đổi trạng thái Post
				post.Status = PostStatus.DA_DUYET.ToString();
				_unitOfWork.Posts.Update(post);

				// 2. TÌM PRODUCT TỪ REDIS THAY VÌ TỪ SQL
				var redisKey = $"ewise:draft_product:{post.PostId}";
				var draftJson = await _redisCacheService.GetStringAsync(redisKey);

				if (!string.IsNullOrEmpty(draftJson))
				{
					var draftData = JsonSerializer.Deserialize<ProductDraftModel>(draftJson, jsonOptions);

					if (draftData != null && draftData.Product != null)
					{
						var product = draftData.Product;
						var productValues = draftData.ProductValues ?? new List<ProductValues>();
						var history = draftData.History;

						product.Status = ProductStatus.CHO_PHAN_KHO.ToString();

						if (history != null)
						{
							history.Status = ProductStatus.CHO_PHAN_KHO.ToString();
							history.ChangedAt = DateTime.UtcNow;
						}

						await _unitOfWork.Products.AddAsync(product);
						foreach (var pv in productValues) await _unitOfWork.ProductValues.AddAsync(pv);
						if (history != null) await _unitOfWork.ProductStatusHistory.AddAsync(history);
						var existingImages = await _unitOfWork.Images.GetsAsync(i => i.PostId == post.PostId);
						foreach (var img in existingImages)
						{
							img.ProductId = product.ProductId; 
							_unitOfWork.Images.Update(img); 
						}
						await _redisCacheService.RemoveAsync(redisKey);
					}
				}
				else
				{
					throw new AppException($"Bài viết {post.PostId} bị mất dữ liệu sản phẩm đính kèm trên Redis. Không thể duyệt.", 400);
				}

				newlyApprovedPosts.Add(post);
			}

			if (newlyApprovedPosts.Any())
			{
				await _notificationService.ProcessApprovalNotificationsAsync(newlyApprovedPosts);
			}

			// Commit xuống Database (Giữ nguyên)
			await _unitOfWork.SaveAsync();

			return true;
		}

		//public async Task<bool> RejectPost(List<Guid> postIds, string rejectMessage)
		//{
		//	//var checkBadWord = await _profanityChecker.ContainsProfanityAsync(rejectMessage);
		//	//if (checkBadWord) throw new AppException("Lý do từ chối chứa từ ngữ không phù hợp.", 400);

		//	var posts = await _unitOfWork.Posts.GetsAsync(p => postIds.Contains(p.PostId), includeProperties: "Product");

		//	if (posts == null || !posts.Any()) throw new AppException("Không tìm thấy bài viết nào hợp lệ.", 404);

		//	var newlyRejectedPosts = new List<Post>();

		//	foreach (var post in posts)
		//	{
		//		if (post.Status == PostStatus.DA_TU_CHOI.ToString()) continue;

		//		post.Status = PostStatus.DA_TU_CHOI.ToString();
		//		post.RejectMessage = rejectMessage;
		//		_unitOfWork.Posts.Update(post);

		//		if (post.Product.ProductId != null && post.Product.ProductId != Guid.Empty)
		//		{
		//			var product = await _unitOfWork.Products.GetByIdAsync(post.Product.ProductId);

		//			if (product != null)
		//			{
		//				product.Status = ProductStatus.DA_TU_CHOI.ToString();
		//				_unitOfWork.Products.Update(product);

		//				var history = new ProductStatusHistory
		//				{
		//					ProductId = post.Product.ProductId,
		//					ChangedAt = DateTime.UtcNow,
		//					Status = ProductStatus.DA_TU_CHOI.ToString(),
		//					StatusDescription = $"Bài đăng bị từ chối. Lý do: {rejectMessage}"
		//				};
		//				await _unitOfWork.ProductStatusHistory.AddAsync(history);
		//			}
		//		}

		//		newlyRejectedPosts.Add(post);
		//	}

		//	if (newlyRejectedPosts.Any())
		//	{
		//		await _notificationService.ProcessRejectionNotificationsAsync(newlyRejectedPosts, rejectMessage);
		//	}

		//	await _unitOfWork.SaveAsync();

		//	return true;
		//}

		public async Task<bool> RejectPost(List<Guid> postIds, string rejectMessage)
		{
			var posts = await _unitOfWork.Posts.GetsAsync(p => postIds.Contains(p.PostId));
			if (posts == null || !posts.Any()) throw new AppException("Không tìm thấy bài viết nào hợp lệ.", 404);

			var newlyRejectedPosts = new List<Post>();

			foreach (var post in posts)
			{
				if (post.Status == PostStatus.DA_TU_CHOI.ToString()) continue;

				post.Status = PostStatus.DA_TU_CHOI.ToString();
				post.RejectMessage = rejectMessage;

				var redisKey = $"ewise:draft_product:{post.PostId}";
				var draftJson = await _redisCacheService.GetStringAsync(redisKey);

				if (!string.IsNullOrEmpty(draftJson))
				{
					if (post.CheckMessage == null) post.CheckMessage = new List<string>();

					post.CheckMessage.Add($"[REJECTED_PRODUCT_DATA]|{draftJson}");

					await _redisCacheService.RemoveAsync(redisKey);
				}

				_unitOfWork.Posts.Update(post);
				newlyRejectedPosts.Add(post);
			}

			if (newlyRejectedPosts.Any())
			{
				await _notificationService.ProcessRejectionNotificationsAsync(newlyRejectedPosts, rejectMessage);
			}

			await _unitOfWork.SaveAsync();
			return true;
		}

		//public async Task<PagedResultModel<PostSummaryModel>> GetPagedPostsAsync(PostSearchQueryModel model)
		//{
		//	if (model == null)
		//	{
		//		throw new AppException("Invalid search model.", 400);
		//	}
		//	string? statusEnum = null;
		//	if (model.Status != null)
		//	{
		//		statusEnum = StatusEnumHelper.GetValueFromDescription<PostStatus>(model.Status).ToString();
		//	}
		//	var (posts, totalItems) = await _postRepository.GetPagedPostsAsync(
		//		status: statusEnum,
		//		search: model.Search,
		//		order: model.Order,
		//		page: model.Page,
		//		limit: model.Limit
		//	);

		//	var summaryList = posts
		//		.Select(p => MapToPostSummaryModel(p))
		//		.ToList();
		//	return new PagedResultModel<PostSummaryModel>(
		//		summaryList,
		//		model.Page,
		//		model.Limit,
		//		totalItems
		//	);
		//}
		public async Task<PagedResultModel<PostSummaryModel>> GetPagedPostsAsync(PostSearchQueryModel model)
		{
			if (model == null) throw new AppException("Invalid search model.", 400);

			string? statusEnum = null;
			if (!string.IsNullOrEmpty(model.Status))
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

			var summaryList = new List<PostSummaryModel>();
			var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			foreach (var p in posts)
			{
				if (p.Status == PostStatus.CHO_DUYET.ToString())
				{
					// 1. Luồng Chờ Duyệt: Lấy data nháp từ Redis
					var redisKey = $"ewise:draft_product:{p.PostId}";
					var draftJson = await _redisCacheService.GetStringAsync(redisKey);

					if (!string.IsNullOrEmpty(draftJson))
					{
						var draftData = JsonSerializer.Deserialize<ProductDraftModel>(draftJson, jsonOptions);
						summaryList.Add(MapDraftToPostSummaryModel(p, draftData));
					}
					else
					{
						summaryList.Add(MapDraftToPostSummaryModel(p, null));
					}
				}
				else if (p.Status == PostStatus.DA_TU_CHOI.ToString() && p.CheckMessage != null && p.CheckMessage.Any(x => x.StartsWith("[REJECTED_PRODUCT_DATA]")))
				{
					// 2. Luồng Từ Chối: Móc chuỗi JSON đã lưu Trick trong CheckMessage
					var draftString = p.CheckMessage.First(x => x.StartsWith("[REJECTED_PRODUCT_DATA]")).Split('|')[1];
					var draftData = JsonSerializer.Deserialize<ProductDraftModel>(draftString, jsonOptions);
					summaryList.Add(MapDraftToPostSummaryModel(p, draftData));
				}
				else
				{
					// 3. Luồng Đã Duyệt: Lấy từ SQL Entity chuẩn của bạn
					// (Giả định hàm MapToPostSummaryModel cũ của bạn đã xử lý đúng với p.Product)
					summaryList.Add(MapToPostSummaryModel(p));
				}
			}

			return new PagedResultModel<PostSummaryModel>(
				summaryList,
				model.Page,
				model.Limit,
				totalItems
			);
		}
		private PostSummaryModel MapDraftToPostSummaryModel(Post post, ProductDraftModel draft)
		{
			var summary = new PostSummaryModel
			{
				Id = post.PostId,
				Date = post.Date,
				Address = post.Address ?? string.Empty,
				SenderName = post.Sender?.Name ?? "Không rõ",
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PostStatus>(post.Status),
				EstimatePoint = post.EstimatePoint,
				ThumbnailUrl = post.Images?.FirstOrDefault()?.ImageUrl ?? string.Empty
			};

			if (draft != null)
			{
				// Gán thẳng tên đã lưu trên Redis vào Model
				summary.Category = draft.CategoryName ?? "Không rõ";
				summary.ChildCategoryName = draft.ChildCategoryName ?? "Không rõ";
				summary.BrandName = draft.BrandName ?? "Không rõ";
			}
			else
			{
				// Xử lý fallback trong trường hợp Redis bị mất data (Hết hạn TTL)
				summary.ThumbnailUrl = string.Empty;
				summary.Category = "Dữ liệu quá hạn";
				summary.ChildCategoryName = "Dữ liệu quá hạn";
				summary.BrandName = "Dữ liệu quá hạn";
			}

			return summary;
		}
		public async Task AutoRejectExpiredPostsAsync()
		{
			var today = DateOnly.FromDateTime(DateTime.Now);
			var pendingPosts = await _unitOfWork.Posts.GetsAsync(p => p.Status == PostStatus.CHO_DUYET.ToString());

			var expiredPostIds = new List<Guid>();
			var message = "Hệ thống tự động từ chối do đã quá hạn hoặc đến ngày thu gom.";

			foreach (var post in pendingPosts)
			{
				if (string.IsNullOrEmpty(post.ScheduleJson)) continue;

				try
				{
					var schedule = JsonSerializer.Deserialize<List<DailyTimeSlots>>(post.ScheduleJson);
					if (schedule != null && schedule.Any(s => s.PickUpDate <= today))
					{
						expiredPostIds.Add(post.PostId);
					}
				}
				catch (JsonException)
				{
					continue;
				}
			}

			if (expiredPostIds.Any())
			{
				await RejectPost(expiredPostIds, message);
			}
		}
	}
}