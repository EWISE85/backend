using DocumentFormat.OpenXml.Spreadsheet;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class ProductService : IProductService
	{
		private readonly IProductRepository _productRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IProductImageRepository _productImageRepository;
		private readonly IPointTransactionService _pointTransactionService;
		private readonly IBrandRepository _brandRepository;
		private readonly ICategoryRepository _categoryRepository;
		private readonly IProductStatusHistoryRepository _productStatusHistoryRepository;
		private readonly IAttributeOptionRepository _attributeOptionRepository;
		private readonly IPackageRepository _packageRepository;
        private readonly CapacityHelper _capacityHelper;
		private readonly IRedisCacheService _redisCacheService;
		public ProductService(IProductRepository productRepository, IUnitOfWork unitOfWork, IProductImageRepository productImageRepository, IPointTransactionService pointTransactionService, IBrandRepository brandRepository, ICategoryRepository categoryRepository, IProductStatusHistoryRepository productStatusHistoryRepository, IAttributeOptionRepository attributeOptionRepository, IPackageRepository packageRepository, CapacityHelper capacityHelper, IRedisCacheService redisCacheService)
		{
			_productRepository = productRepository;
			_unitOfWork = unitOfWork;
			_productImageRepository = productImageRepository;
			_pointTransactionService = pointTransactionService;
			_brandRepository = brandRepository;
			_categoryRepository = categoryRepository;
			_productStatusHistoryRepository = productStatusHistoryRepository;
			_attributeOptionRepository = attributeOptionRepository;
			_packageRepository = packageRepository;
			_capacityHelper = capacityHelper;
			_redisCacheService = redisCacheService;
		}



		public async Task<bool> AddPackageIdToProductByQrCode(string qrCode, string? packageId)
		{
			var product = await _productRepository.GetAsync(p => p.QRCode == qrCode);
			if (product == null) throw new AppException("Không tìm thấy sản phẩm",404);
			product.PackageId = packageId;
			product.Status = ProductStatus.DA_DONG_THUNG.ToString();
			_unitOfWork.Products.Update(product);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<ProductDetailModel> AddProduct(CreateProductAtWarehouseModel createProductRequest)
		{
			var existingProduct = await _productRepository.GetAsync(p => p.QRCode == createProductRequest.QrCode);
			if (existingProduct != null)
			{
				throw new AppException("Sản phẩm với mã QR này đã tồn tại.", 400);
			}
			var newProduct = new Products
			{
				ProductId = Guid.NewGuid(),
				CategoryId = createProductRequest.SubCategoryId,
				UserId = createProductRequest.SenderId ?? Guid.Empty,
				BrandId = createProductRequest.BrandId,
				Description = createProductRequest.Description,
				QRCode = createProductRequest.QrCode,
				CreateAt = DateOnly.FromDateTime(DateTime.UtcNow),
                CollectionUnitId = createProductRequest.SmallCollectionPointId,
				isChecked = false,
				Status = ProductStatus.NHAP_KHO.ToString()
			};
			await _unitOfWork.Products.AddAsync(newProduct);

			var productImages = new List<Image>();
			for (int i = 0; i < createProductRequest.Images.Count; i++)
			{
				var newProductImage = new Image
				{
					ImageUrl = createProductRequest.Images[i],
					ProductId = newProduct.ProductId,
					Id = Guid.NewGuid()
				};
				productImages.Add(newProductImage);
				await _unitOfWork.Images.AddAsync(newProductImage);
			}
			
			if (createProductRequest.SenderId.HasValue)
			{
				var pointTransaction = new CreatePointTransactionModel
				{
					UserId = createProductRequest.SenderId.Value,
					Point = createProductRequest.Point,
					ProductId = newProduct.ProductId,
					Desciption = "Điểm nhận được khi gửi sản phẩm tại kho",
				};
				 await _pointTransactionService.ReceivePointFromCollectionPoint(pointTransaction,false);
			}
			var newHistory = new ProductStatusHistory
			{
				ProductStatusHistoryId = Guid.NewGuid(),
				ProductId = newProduct.ProductId,
				ChangedAt = DateTime.UtcNow,
				StatusDescription = "Sản phẩm đã nhập kho",
				Status = ProductStatus.NHAP_KHO.ToString()
			};
			await _unitOfWork.ProductStatusHistory.AddAsync(newHistory);
			await _unitOfWork.SaveAsync();
			return await BuildProductDetailModelAsync(newProduct);
		}

		private async Task<ProductDetailModel> BuildProductDetailModelAsync(Products product)
		{
			var brand = await _brandRepository.GetByIdAsync(product.BrandId);
			var category = await _categoryRepository.GetByIdAsync(product.CategoryId);
			return new ProductDetailModel
			{
				ProductId = product.ProductId,
				Description = product.Description,
				CategoryId = product.CategoryId,
				BrandId = product.BrandId,
				BrandName = brand?.Name,
				CategoryName = category?.Name,
				QrCode = product.QRCode,
				IsChecked = product.isChecked,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(product.Status)
			};
		}

		public async Task<ProductDetailModel> GetById(Guid productId)
		{
			var product = await _productRepository.GetAsync(p => p.ProductId == productId);
			if (product == null) throw new AppException("Không tìm thấy sản phẩm", 404);
			return await BuildProductDetailModelAsync(product);
		}

		public async Task<ProductComeWarehouseDetailModel> GetByQrCode(string qrcode)
		{
			var product = await _productRepository.GetProductByQrCodeWithDetailsAsync(qrcode);

			if (product == null) throw new AppException("Không tìm thấy sản phẩm với mã QR đã cho", 404);

            var post = product.Post;

            var imageUrls = product.Images?.Select(img => img.ImageUrl).ToList() ?? new List<string>();
			return new ProductComeWarehouseDetailModel
			{
				ProductId = product.ProductId,
				Description = product.Description,
				BrandId = product.BrandId,
				BrandName = product.Brand?.Name ?? "N/A",
				CategoryId = product.CategoryId,
				CategoryName = product.Category?.Name ?? "N/A",
				ProductImages = imageUrls,
				QrCode = product.QRCode,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(product.Status),
				EstimatePoint = post?.EstimatePoint,
				RealPoint = product.PointTransactions?.FirstOrDefault()?.Point,
			};
		}

		public async Task<List<ProductDetailModel>> GetProductsByPackageIdAsync(string packageId)
		{
			var products = await _productRepository.GetProductsByPackageIdWithDetailsAsync(packageId);

			if (products == null || !products.Any())
			{
				return new List<ProductDetailModel>();
			}

			var resultList = new List<ProductDetailModel>();

			foreach (var p in products)
			{
				List<ProductValueDetailModel> attributesList = new List<ProductValueDetailModel>();

				if (p.ProductValues != null)
				{
					foreach (var pv in p.ProductValues)
					{
						ProductValueDetailModel detail;
						if (pv.AttributeOptionId.HasValue)
						{
							detail = await MapProductValueDetailWithOptionAsync(pv);
						}
						else
						{
							detail = MapProductValueDetail(pv, null);
						}
						attributesList.Add(detail);
					}
				}

				var model = new ProductDetailModel
				{
					ProductId = p.ProductId,
					Description = p.Description,
					BrandName = p.Brand?.Name,
					BrandId = p.BrandId,
					CategoryId = p.CategoryId,
					CategoryName = p.Category?.Name,
					QrCode = p.QRCode,
					Attributes = attributesList, 
					IsChecked = p.isChecked,
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(p.Status)
				};

				resultList.Add(model);
			}

			return resultList;
		}

		private ProductValueDetailModel MapProductValueDetail(ProductValues pv, AttributeOptions? option)
		{
			return new ProductValueDetailModel
			{
				AttributeId = pv.AttributeId ?? Guid.Empty,
				AttributeName = pv.Attribute?.Name ?? "Unknown",
				Value = pv.Value?.ToString(),
				OptionName = option?.OptionName
			};
		}

		public async Task<List<ProductComeWarehouseDetailModel>> ProductsComeWarehouseByDateAsync(DateOnly fromDate, DateOnly toDate, string smallCollectionPointId)
		{
			var productsFromRoutesTask = await _productRepository.GetProductsCollectedByRouteAsync(fromDate, toDate, smallCollectionPointId);
			var directProductsTask = await _productRepository.GetDirectlyEnteredProductsAsync(fromDate, toDate, smallCollectionPointId);
			var productsFromRoutes = productsFromRoutesTask;
			var directProducts = directProductsTask;
			var combinedProducts = productsFromRoutes
				.Concat(directProducts)
				.DistinctBy(p => p.ProductId)
				.ToList();

			var combinedList = combinedProducts.Select(product =>
			{
				var post = product.Post;
				return MapToDetailModel(product, post);
			})
			.Where(x => x != null)
			.OrderByDescending(x => x.Status)
			.ToList();
			return combinedList;
		}




		private ProductComeWarehouseDetailModel MapToDetailModel(Products product, Post? post)
		{
			return new ProductComeWarehouseDetailModel
			{
				ProductId = product.ProductId,
				CategoryName = product.Category?.Name ?? "N/A", // Đã là Cate con trong DB
				BrandName = product.Brand?.Name ?? "N/A",
				Description = product.Description ?? post?.Description ?? "Không có mô tả",
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(product.Status),

				// Ưu tiên ngày tạo của Product, nếu không có mới lấy ngày của Post
				CreateAt = product.CreateAt.HasValue
					? product.CreateAt.Value.ToDateTime(TimeOnly.MinValue)
					: (post?.Date ?? DateTime.MinValue),

				// Gộp ảnh từ cả Post (mới) và Product (cũ/trực tiếp)
				ProductImages = (post?.Images?.Select(i => i.ImageUrl) ?? new List<string>())
								.Union(product.Images?.Select(i => i.ImageUrl) ?? new List<string>())
								.Distinct()
								.ToList(),

				UserName = product.User?.Name ?? post?.Sender?.Name ?? "N/A"
			};
		}

		public async Task<bool> UpdateProductStatusByQrCode(string productQrCode, string status)
		{
			var product = await _productRepository.GetAsync(p => p.QRCode == productQrCode);
			if (product == null) throw new AppException("Không tìm thấy sản phẩm với mã QR đã cho", 404);
			product.Status = status;
			_unitOfWork.Products.Update(product);
			await _unitOfWork.SaveAsync();
			return true;
		}

        //public async Task<bool> UpdateProductStatusByQrCodeAndPlusUserPoint(string productQrCode, string status)
        //{
        //    var product = await _unitOfWork.Products.GetAsync(p => p.QRCode == productQrCode);
        //    if (product == null) throw new AppException("Không tìm thấy sản phẩm với mã QR đã cho", 404);


        //    var post = await _unitOfWork.Posts.GetAsync(p => p.ProductId == product.ProductId);
        //    if (post == null) throw new AppException("Không tìm thấy bài đăng liên quan đến sản phẩm", 404);
        //    var pointTransaction = new CreatePointTransactionModel
        //    {
        //        UserId = product.UserId,
        //        ProductId = product.ProductId,
        //        Point = post.EstimatePoint,
        //        Desciption = "Sản phầm đã về đến kho"
        //    };
        //    var statusEnum = StatusEnumHelper.GetValueFromDescription<ProductStatus>(status);
        //    product.Status = statusEnum.ToString();
        //    _unitOfWork.Products.Update(product);
        //    var newHistory = new ProductStatusHistory
        //    {
        //        ProductStatusHistoryId = Guid.NewGuid(),
        //        ProductId = product.ProductId,
        //        ChangedAt = DateTime.UtcNow,
        //        StatusDescription = "Sản phẩm đã về đến kho",
        //        Status = statusEnum.ToString()
        //    };
        //    await _unitOfWork.ProductStatusHistory.AddAsync(newHistory);
        //    await _pointTransactionService.ReceivePointFromCollectionPoint(pointTransaction, false);
        //    await _unitOfWork.SaveAsync();
        //    return true;
        //}

        //      public async Task<bool> ReceiveProductAtWarehouse(List<UserReceivePointFromCollectionPointModel> models)
        //      {
        //	if (models == null || !models.Any()) return false;
        //	string pointIdToSync = null;

        //	foreach (var model in models)
        //	{
        //		var product = await _unitOfWork.Products.GetAsync(p => p.QRCode == model.QRCode);
        //		if (product == null) throw new AppException($"Không tìm thấy sản phẩm với mã QR: {model.QRCode}", 404);
        //		if (string.IsNullOrEmpty(pointIdToSync))
        //		{
        //			pointIdToSync = product.SmallCollectionPointId;
        //		}

        //		var post = await _unitOfWork.Posts.GetAsync(p => p.ProductId == product.ProductId);
        //		if (post == null) throw new AppException($"Không tìm thấy bài đăng liên quan đến sản phẩm mã QR: {model.QRCode}", 404);
        //		double pointToSave = model.Point ?? post.EstimatePoint;
        //		string descriptionToSave = !string.IsNullOrEmpty(model.Description) ? model.Description : "Sản phẩm đã về đến kho";
        //		var pointTransaction = new CreatePointTransactionModel
        //		{
        //			UserId = product.UserId,
        //			ProductId = product.ProductId,
        //			Point = pointToSave,
        //			Desciption = descriptionToSave
        //		};

        //		product.Status = ProductStatus.NHAP_KHO.ToString();
        //		_unitOfWork.Products.Update(product);
        //		var newHistory = new ProductStatusHistory
        //		{
        //			ProductStatusHistoryId = Guid.NewGuid(),
        //			ProductId = product.ProductId,
        //			ChangedAt = DateTime.UtcNow,
        //			StatusDescription = descriptionToSave,
        //			Status = ProductStatus.NHAP_KHO.ToString()
        //		};
        //		await _unitOfWork.ProductStatusHistory.AddAsync(newHistory);

        //		await _pointTransactionService.ReceivePointFromCollectionPoint(pointTransaction, false);
        //	}

        //	await _unitOfWork.SaveAsync();

        //	if (!string.IsNullOrEmpty(pointIdToSync))
        //	{
        //		await _capacityHelper.SyncRealtimeCapacityAsync(pointIdToSync);
        //	}

        //	return true;
        //}


        public async Task<bool> ReceiveProductAtWarehouse(List<UserReceivePointFromCollectionPointModel> models)
        {
            if (models == null || !models.Any()) return false;

            var pointIdsToSync = new HashSet<string>();

            foreach (var model in models)
            {
                var product = await _unitOfWork.Products.GetAsync(p => p.QRCode == model.QRCode);
                if (product == null) throw new AppException($"Không tìm thấy sản phẩm với mã QR: {model.QRCode}", 404);

                if (!string.IsNullOrEmpty(product.CollectionUnitId))
                {
                    pointIdsToSync.Add(product.CollectionUnitId);
                }

                var post = await _unitOfWork.Posts
                    .GetAsync(p => p.Product != null && p.Product.ProductId == product.ProductId);
				if (post == null) throw new AppException($"Không tìm thấy bài đăng liên quan đến sản phẩm mã QR: {model.QRCode}", 404);

                double pointToSave = model.Point ?? post.EstimatePoint;
                string descriptionToSave = !string.IsNullOrEmpty(model.Description) ? model.Description : "Sản phẩm đã về đến kho";

                var pointTransaction = new CreatePointTransactionModel
                {
                    UserId = product.UserId,
                    ProductId = product.ProductId,
                    Point = pointToSave,
                    Desciption = descriptionToSave
                };

                product.Status = ProductStatus.NHAP_KHO.ToString();
                _unitOfWork.Products.Update(product);

                var newHistory = new ProductStatusHistory
                {
                    ProductStatusHistoryId = Guid.NewGuid(),
                    ProductId = product.ProductId,
                    ChangedAt = DateTime.UtcNow,
                    StatusDescription = descriptionToSave,
                    Status = ProductStatus.NHAP_KHO.ToString()
                };
                await _unitOfWork.ProductStatusHistory.AddAsync(newHistory);

                await _pointTransactionService.ReceivePointFromCollectionPoint(pointTransaction, false);
            }

            await _unitOfWork.SaveAsync();

            foreach (var pointId in pointIdsToSync)
            {
                await _capacityHelper.SyncRealtimeCapacityAsync(pointId);
            }

            return true;
        }


		//      public async Task<PagedResultModel<ProductComeWarehouseDetailModel>> GetAllProductsByUserId(string? search, DateOnly? createAt, Guid userId, int page, int limit)
		//{
		//	var (products, totalItems) = await _productRepository.GetProductsBySenderIdWithDetailsAsync(search, createAt, userId, page, limit);

		//	if (products == null || !products.Any())
		//	{
		//		return new PagedResultModel<ProductComeWarehouseDetailModel>(new List<ProductComeWarehouseDetailModel>(), page, limit, 0);
		//	}

		//	// Mapping sang DetailModel
		//	var productDetails = products.Select(product =>
		//	{
		//              // Lấy post liên quan đến user này (nếu có logic đặc thù)
		//              var post = product.Post?.SenderId == userId
		//                  ? product.Post
		//                  : null; 
		//		return MapToDetailModel(product, post);
		//	})
		//	.Where(x => x != null)
		//	.ToList();

		//	// Trả về kết quả bọc trong PagedResultModel
		//	return new PagedResultModel<ProductComeWarehouseDetailModel>(productDetails, page, limit, totalItems);
		//}

		public async Task<PagedResultModel<ProductComeWarehouseDetailModel>> GetAllProductsByUserId(string? search, DateOnly? createAt, Guid userId, int page, int limit)
		{
			var allProductDetails = new List<ProductComeWarehouseDetailModel>();
			var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			// BƯỚC 1: LẤY DỮ LIỆU TỪ 2 NGUỒN
			// Nguồn 1: Tất cả Posts (Chờ duyệt, Đã duyệt, Đã từ chối...)
			var allPosts = await _unitOfWork.Posts.GetsAsync(
				p => p.SenderId == userId,
				includeProperties: "Sender,Product,Product.Category,Product.Brand,Images,Product.Images"
			);

			// Nguồn 2: Sản phẩm nhập trực tiếp tại kho (Không có PostId)
			var directProducts = await _unitOfWork.Products.GetsAsync(
				p => p.UserId == userId && p.PostId == null,
				includeProperties: "Category,Brand,Images,User"
			);

			// BƯỚC 2: XỬ LÝ NGUỒN TỪ POSTS
			if (allPosts != null && allPosts.Any())
			{
				// Lọc theo ngày nếu có
				var filteredPosts = createAt.HasValue
					? allPosts.Where(p => DateOnly.FromDateTime(p.Date) == createAt.Value).ToList()
					: allPosts.ToList();

				foreach (var post in filteredPosts)
				{
					if (post.Product != null) // Nhánh đã có Product trong SQL
					{
						if (!string.IsNullOrEmpty(search))
						{
							string s = search.ToLower();
							bool match = (post.Product.Category?.Name?.ToLower()?.Contains(s) == true) ||
										 (post.Description?.ToLower()?.Contains(s) == true);
							if (!match) continue;
						}
						allProductDetails.Add(MapToDetailModel(post.Product, post));
					}
					else // Nhánh Draft (Redis)
					{
						string? draftJson = null;
						if (post.Status == PostStatus.CHO_DUYET.ToString())
							draftJson = await _redisCacheService.GetStringAsync($"ewise:draft_product:{post.PostId}");
						else if (post.CheckMessage != null)
						{
							var trick = post.CheckMessage.FirstOrDefault(x => x.StartsWith("[REJECTED_PRODUCT_DATA]"));
							if (trick != null) draftJson = trick.Split('|')[1];
						}

						var draftData = !string.IsNullOrEmpty(draftJson) ? JsonSerializer.Deserialize<ProductDraftModel>(draftJson, jsonOptions) : null;

						if (!string.IsNullOrEmpty(search) && draftData != null)
						{
							string s = search.ToLower();
							bool match = (draftData.ChildCategoryName?.ToLower()?.Contains(s) == true) ||
										 (post.Description?.ToLower()?.Contains(s) == true);
							if (!match) continue;
						}
						allProductDetails.Add(MapDraftToWarehouseDetailModel(post, draftData));
					}
				}
			}

			// BƯỚC 3: XỬ LÝ NGUỒN SẢN PHẨM TRỰC TIẾP (Không qua Post)
			if (directProducts != null && directProducts.Any())
			{
				foreach (var product in directProducts)
				{
					// Lọc theo ngày
					if (createAt.HasValue && product.CreateAt != createAt.Value) continue;

					// Lọc theo search
					if (!string.IsNullOrEmpty(search))
					{
						string s = search.ToLower();
						bool match = (product.Category?.Name?.ToLower()?.Contains(s) == true) ||
									 (product.Description?.ToLower()?.Contains(s) == true);
						if (!match) continue;
					}

					// Map với post = null
					allProductDetails.Add(MapToDetailModel(product, null));
				}
			}

			// BƯỚC 4: SẮP XẾP, PHÂN TRANG VÀ TRẢ VỀ
			var totalItems = allProductDetails.Count;
			var pagedResult = allProductDetails
				.OrderByDescending(x => x.CreateAt)
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToList();

			return new PagedResultModel<ProductComeWarehouseDetailModel>(pagedResult, page, limit, totalItems);
		}
		private ProductComeWarehouseDetailModel MapDraftToWarehouseDetailModel(Post post, ProductDraftModel? draft)
		{
			return new ProductComeWarehouseDetailModel
			{
				// Nếu là bài đăng mới, lấy ProductId ảo từ draft để Tracking
				ProductId = draft?.Product?.ProductId ?? post.PostId,

				CategoryName = draft?.ChildCategoryName ?? "Dữ liệu đang chờ duyệt", // Luôn lấy Cate con
				BrandName = draft?.BrandName ?? "Không rõ",
				Description = post.Description ?? "Không có mô tả",
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PostStatus>(post.Status),
				CreateAt = post.Date,

				// Draft thì lấy ảnh từ Post.Images (vì chưa có Product SQL)
				ProductImages = post.Images?.Select(i => i.ImageUrl).ToList() ?? new List<string>(),
				EstimatePoint = post.EstimatePoint,
				UserName = post.Sender?.Name ?? "N/A"
			};
		}
		//public async Task<ProductDetail?> GetProductDetailByIdAsync(Guid productId)
		//{
		//	var product = await _productRepository.GetProductDetailWithAllRelationsAsync(productId);
		//	if (product == null) return null;

		//	var post = product.Post;

		//	List<ProductValueDetailModel> productAttributes = new List<ProductValueDetailModel>();
		//	if (product.ProductValues != null)
		//	{
		//		foreach (var pv in product.ProductValues)
		//		{
		//			ProductValueDetailModel detail;
		//			if (pv.AttributeOptionId.HasValue)
		//			{
		//				detail = await MapProductValueDetailWithOptionAsync(pv);
		//			}
		//			else
		//			{
		//				detail = MapProductValueDetail(pv, null);
		//			}
		//			productAttributes.Add(detail);
		//		}
		//	}
		//	var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		//	List<DailyTimeSlots> schedule = new List<DailyTimeSlots>();
		//	if (post != null)
		//	{
		//		if (!string.IsNullOrEmpty(post.ScheduleJson))
		//		{
		//			try { schedule = JsonSerializer.Deserialize<List<DailyTimeSlots>>(post.ScheduleJson, options) ?? new List<DailyTimeSlots>(); }
		//			catch (JsonException) { schedule = new List<DailyTimeSlots>(); }
		//		}
		//	}

		//	var route = product.CollectionRoutes?.FirstOrDefault();
		//	var shifts = route?.CollectionGroup?.Shifts;
		//	var senderId = product.UserId;
		//	var collector = shifts?.Collector;
		//	var realPoint = product.PointTransactions?.FirstOrDefault()?.Point;
		//	var sender = await _unitOfWork.Users.GetAsync(u => u.UserId == senderId);
		//	if (sender == null) throw new AppException("Không tìm thấy người gửi", 404);

		//	var userResponse = new UserResponse
		//	{
		//		UserId = sender?.UserId ?? Guid.Empty,
		//		Name = sender?.Name,
		//		Phone = sender?.Phone,
		//		Email = sender?.Email,
		//		Avatar = sender?.Avatar,
		//		Role = sender.Role,
		//		SmallCollectionPointId = sender?.CollectionUnitId
		//          };

		//	double? realPoints = null;
		//	string? changedPointMessage = null;

		//	if (product.PointTransactions != null && product.PointTransactions.Any())
		//	{
		//		realPoints = product.PointTransactions.Sum(pt => pt.Point);

		//		var latestTransaction = product.PointTransactions
		//			.OrderByDescending(pt => pt.CreatedAt)
		//			.FirstOrDefault();

		//		if (latestTransaction != null && latestTransaction.TransactionType == PointTransactionType.DIEU_CHINH.ToString())
		//		{
		//			changedPointMessage = latestTransaction.Desciption;
		//		}
		//	}

		//	return new ProductDetail
		//	{
		//		ProductId = product.ProductId,
		//		CategoryId = product.CategoryId,
		//		CategoryName = product.Category?.Name ?? "Không rõ",
		//		BrandId = product.BrandId,
		//		BrandName = product.Brand?.Name ?? "Không rõ",
		//		Description = product.Description,
		//		ProductImages = product.ProductImages?.Select(pi => pi.ImageUrl).ToList() ?? new List<string>(),
		//		Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(product.Status),
		//		EstimatePoint = post?.EstimatePoint,
		//		Sender = userResponse,
		//		Address = post?.Address ?? "Không có địa chỉ",
		//		Schedule = schedule,
		//		Attributes = productAttributes,
		//		RejectMessage = post?.RejectMessage ?? "Không có lý do",
		//		QRCode = product.QRCode,
		//		IsChecked = product.isChecked,
		//		RealPoints = realPoints,
		//		Collector = collector != null ? new CollectorResponse
		//		{
		//			CollectorId = collector.UserId,
		//			Name = collector.Name
		//		} : null,
		//		PickUpDate = route?.CollectionDate,
		//		EstimatedTime = route?.EstimatedTime,
		//		CollectionRouterId = route?.CollectionRouteId,
		//		ChangedPointMessage = changedPointMessage,


		//	};
		//}
		public async Task<ProductDetail?> GetProductDetailByIdAsync(Guid id)
		{
			var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			// BƯỚC 1: Tìm Post (Thử tìm theo PostId hoặc ProductId đã có trong SQL)
			var post = await _unitOfWork.Posts.GetAsync(
				p => p.PostId == id || (p.Product != null && p.Product.ProductId == id),
				includeProperties: "Sender,Images,Product,Product.Category,Product.Brand,Product.PointTransactions,Product.ProductValues,Product.ProductValues.Attribute"
			);

			// BƯỚC 2: Tra từ điển Redis nếu không thấy trong SQL
			if (post == null)
			{
				var mappedPostIdStr = await _redisCacheService.GetStringAsync($"ewise:product_map:{id}");
				if (!string.IsNullOrEmpty(mappedPostIdStr) && Guid.TryParse(mappedPostIdStr, out var mappedPostId))
				{
					// [ĐÃ SỬA] Dùng mappedPostId để tìm bài Post gốc
					post = await _unitOfWork.Posts.GetAsync(
						p => p.PostId == mappedPostId,
						includeProperties: "Sender,Images,Product,Product.Category,Product.Brand,Product.PointTransactions,Product.ProductValues,Product.ProductValues.Attribute"
					);
				}
			}

			if (post != null)
			{
				if (post.Product != null)
				{
					// Nhánh SQL (Đã duyệt)
					var productFull = await _productRepository.GetProductDetailWithAllRelationsAsync(post.Product.ProductId);
					return await MapSqlProductToDetail(productFull ?? post.Product, post, jsonOptions);
				}
				else
				{
					// Nhánh Draft (Chờ duyệt / Bị từ chối)
					string? draftJson = null;

					if (post.Status == PostStatus.CHO_DUYET.ToString())
					{
						draftJson = await _redisCacheService.GetStringAsync($"ewise:draft_product:{post.PostId}");
					}

					if (string.IsNullOrEmpty(draftJson) && post.CheckMessage != null)
					{
						var trick = post.CheckMessage.FirstOrDefault(x => x.StartsWith("[REJECTED_PRODUCT_DATA]"));
						if (trick != null) draftJson = trick.Split('|')[1];
					}

					var draftData = !string.IsNullOrEmpty(draftJson)
						? JsonSerializer.Deserialize<ProductDraftModel>(draftJson, jsonOptions)
						: null;

					return await MapDraftProductToDetail(draftData, post, jsonOptions);
				}
			}

			// BƯỚC 3: Fallback cho hàng nhập kho trực tiếp (Không qua bài đăng)
			var directProduct = await _productRepository.GetProductDetailWithAllRelationsAsync(id);
			if (directProduct != null)
			{
				return await MapSqlProductToDetail(directProduct, directProduct.Post, jsonOptions);
			}

			return null;
		}
		private async Task<ProductDetail> MapSqlProductToDetail(Products product, Post? post, JsonSerializerOptions options)
		{
			// Map Attributes
			List<ProductValueDetailModel> productAttributes = new List<ProductValueDetailModel>();
			if (product.ProductValues != null)
			{
				foreach (var pv in product.ProductValues)
				{
					productAttributes.Add(pv.AttributeOptionId.HasValue
						? await MapProductValueDetailWithOptionAsync(pv)
						: MapProductValueDetail(pv, null));
				}
			}

			// Map Schedule từ Post
			List<DailyTimeSlots> schedule = new List<DailyTimeSlots>();
			if (post != null && !string.IsNullOrEmpty(post.ScheduleJson))
			{
				try { schedule = JsonSerializer.Deserialize<List<DailyTimeSlots>>(post.ScheduleJson, options) ?? new List<DailyTimeSlots>(); } catch { }
			}

			// Map Thông tin vận chuyển
			var route = product.CollectionRoutes?.OrderByDescending(r => r.CollectionDate).FirstOrDefault();
			var collector = route?.CollectionGroup?.Shifts?.Collector;

			// Map Thông tin người gửi (Xử lý khi post null)
			var sender = post?.Sender;
			var userResponse = new UserResponse
			{
				UserId = sender?.UserId ?? product.UserId,
				Name = sender?.Name ?? "Nhập trực tiếp tại kho",
				Phone = sender?.Phone ?? "N/A",
				Email = sender?.Email,
				Avatar = sender?.Avatar,
				Role = sender?.Role ?? "User"
			};

			// Tính điểm thực nhận
			double? realPoints = product.PointTransactions?.Sum(pt => pt.Point);
			string? changedPointMessage = product.PointTransactions?
				.OrderByDescending(pt => pt.CreatedAt)
				.FirstOrDefault(pt => pt.TransactionType == PointTransactionType.DIEU_CHINH.ToString())?.Desciption;

			return new ProductDetail
			{
				ProductId = product.ProductId,
				CategoryName = product.Category?.Name ?? "Không rõ",
				BrandName = product.Brand?.Name ?? "Không rõ",
				Description = product.Description,
				ProductImages = post?.Images?.Select(i => i.ImageUrl).ToList()
					 ?? product?.Images?.Select(i => i.ImageUrl).ToList()
					 ?? new List<string>(),
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(product.Status),
				EstimatePoint = post?.EstimatePoint,
				Sender = userResponse,
				Address = post?.Address ?? "Tại kho",
				Schedule = schedule,
				Attributes = productAttributes,
				QRCode = product.QRCode,
				RealPoints = realPoints,
				Collector = collector != null ? new CollectorResponse { CollectorId = collector.UserId, Name = collector.Name } : null,
				PickUpDate = route?.CollectionDate,
				ChangedPointMessage = changedPointMessage,
				RejectMessage = post?.RejectMessage ?? "Không có"
			};
		}
		private async Task<ProductDetail> MapDraftProductToDetail(ProductDraftModel? draft, Post post, JsonSerializerOptions options)
		{
			var productAttributes = new List<ProductValueDetailModel>();

			if (draft?.ProductValues != null)
			{
				foreach (var pv in draft.ProductValues)
				{
					// FIX: Truy vấn tên thuộc tính từ DB vì JSON Redis không lưu Name
					if (pv.AttributeId.HasValue && pv.Attribute == null)
					{
						pv.Attribute = await _unitOfWork.Attributes.GetByIdAsync(pv.AttributeId.Value);
					}

					if (pv.AttributeOptionId.HasValue)
						productAttributes.Add(await MapProductValueDetailWithOptionAsync(pv));
					else
						productAttributes.Add(MapProductValueDetail(pv, null));
				}
			}

			return new ProductDetail
			{
				// GIỮ NGUYÊN PRODUCT ID THẬT ĐỂ TRACKING
				ProductId = draft?.Product?.ProductId ?? Guid.Empty,
				CategoryName = draft?.ChildCategoryName ?? "Dữ liệu đang chờ duyệt",
				BrandName = draft?.BrandName ?? "Không rõ",
				Description = post.Description ?? draft?.Product?.Description ?? string.Empty,
				ProductImages = post.Images?.Select(pi => pi.ImageUrl).ToList() ?? new List<string>(),
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PostStatus>(post.Status),
				EstimatePoint = post.EstimatePoint,
				Address = post.Address,
				Attributes = productAttributes,
				Sender = new UserResponse
				{
					UserId = post.Sender?.UserId ?? Guid.Empty,
					Name = post.Sender?.Name,
					Phone = post.Sender?.Phone,
					Avatar = post.Sender?.Avatar
				},
				// FIX: Chỉ hiện Reject Message khi status thực sự là DA_TU_CHOI
				RejectMessage = (post.Status == PostStatus.DA_TU_CHOI.ToString())
								? (post.RejectMessage ?? "Không có lý do cụ thể")
								: null
			};
		}
		private async Task<ProductValueDetailModel> MapProductValueDetailWithOptionAsync(ProductValues pv)
		{
			var option = await _attributeOptionRepository.GetAsync(ao => ao.OptionId == pv.AttributeOptionId.Value);
			return MapProductValueDetail(pv, option);
		}

		public async Task<bool> UpdateProductStatusByProductId(Guid productId, string status)
		{
			var product = await _productRepository.GetAsync(p => p.ProductId == productId);
			if (product == null) throw new AppException("Không tìm thấy sản phẩm với Id đã cho", 404);
			product.Status = status;
			_unitOfWork.Products.Update(product);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<bool> UpdateCheckedProductAtRecycler(string packageId, List<string> QrCode)
		{
			var package = await _packageRepository.GetAsync(p => p.PackageId == packageId);
			if (package == null) throw new AppException("Không tìm thấy gói hàng với Id đã cho", 404);
			foreach (var qrCode in QrCode)
			{
				var product = await _productRepository.GetAsync(p => p.QRCode == qrCode && p.PackageId == packageId);
				if (product != null)
				{
					product.isChecked = true;
					_unitOfWork.Products.Update(product);
				}
			}
			await _unitOfWork.SaveAsync();
			return true;

		}

	

		public async Task<PagedResultModel<ProductDetail>> AdminGetProductsAsync(AdminFilterProductModel model)
		{

			var (productsPaged, totalRecords) = await _productRepository.GetPagedProductsForAdminAsync(
				page: model.Page,
				limit: model.Limit,
				fromDate: model.FromDate,
				toDate: model.ToDate,
				categoryName: model.CategoryName,
				collectionCompanyId: model.CollectionCompanyId
			);

			var productDetails = productsPaged.Select(product =>
			{
				
				var post = product.Post;
				var route = product.CollectionRoutes?.FirstOrDefault();
				var shifts = route?.CollectionGroup?.Shifts;
				var sender = post?.Sender;
				var collector = shifts?.Collector;

				var userAddress = sender?.UserAddresses?.FirstOrDefault(ua => ua.Address == post?.Address);

				var realPoint = product.PointTransactions?.FirstOrDefault()?.Point;

				var userResponse = new UserResponse
				{
					UserId = sender?.UserId ?? Guid.Empty,
					Name = sender?.Name,
					Phone = sender?.Phone,
					Email = sender?.Email,
					Avatar = sender?.Avatar,
					Role = sender?.Role,
					SmallCollectionPointId = sender?.CollectionUnitId
                };

				return new ProductDetail
				{
					ProductId = product.ProductId,
					CategoryId = product.CategoryId,
					BrandId = product.BrandId,
					CollectionRouterId = route?.CollectionRouteId,
					EstimatePoint = post?.EstimatePoint,
					QRCode = product.QRCode,
					IsChecked = product.isChecked,
					RealPoints = realPoint,

					CategoryName = product.Category?.Name ?? "Không rõ",
					Description = product.Description,
					BrandName = product.Brand?.Name ?? "Không rõ",
					ProductImages = product.Images?.Select(pi => pi.ImageUrl).ToList() ?? new List<string>(),
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(product.Status),
					Sender = userResponse,
					Address = userAddress?.Address ?? post?.Address ?? "N/A",

					Collector = collector != null ? new CollectorResponse { CollectorId = collector.UserId, Name = collector.Name } : null,
					PickUpDate = route?.CollectionDate,
					EstimatedTime = route?.EstimatedTime,
				};
			})
			.Where(pd => pd != null) 
			.ToList();

			return new PagedResultModel<ProductDetail>(productDetails, model.Page, model.Limit, totalRecords);
		}

		public async Task<bool> CancelProduct(Guid id, string rejectMessage)
		{
			var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			// 1. TÌM BÀI POST (Ưu tiên tìm trong SQL thông qua quan hệ Product trước)
			// Vì id là ProductId, nên p.Product.ProductId == id sẽ bắt được các bài Đã duyệt
			var post = await _unitOfWork.Posts.GetAsync(
				p => (p.Product != null && p.Product.ProductId == id) || p.PostId == id,
				includeProperties: "Product"
			);

			// 2. NẾU KHÔNG THẤY TRONG SQL (Có thể là ProductId của bài nháp trên Redis)
			if (post == null)
			{
				var mappedPostIdStr = await _redisCacheService.GetStringAsync($"ewise:product_map:{id}");
				if (!string.IsNullOrEmpty(mappedPostIdStr) && Guid.TryParse(mappedPostIdStr, out var mappedPostId))
				{
					post = await _unitOfWork.Posts.GetAsync(p => p.PostId == mappedPostId, includeProperties: "Product");
				}
			}

			if (post == null) throw new AppException("Không tìm thấy bài đăng hoặc sản phẩm để hủy", 404);

			// =============================================================
			// NHÁNH 1: SẢN PHẨM ĐÃ CÓ TRONG SQL (Đã duyệt / Chờ thu gom...)
			// =============================================================
			if (post.Product != null)
			{
				var product = post.Product;

				post.Status = PostStatus.DA_HUY.ToString();
				product.Status = ProductStatus.DA_HUY.ToString();
				post.RejectMessage = rejectMessage;

				// Lưu lịch sử thay đổi trạng thái
				var newHistory = new ProductStatusHistory
				{
					ProductStatusHistoryId = Guid.NewGuid(),
					ProductId = product.ProductId,
					ChangedAt = DateTime.UtcNow,
					StatusDescription = "Người dùng đã hủy sản phẩm",
					Status = ProductStatus.DA_HUY.ToString()
				};

				_unitOfWork.Products.Update(product);
				await _unitOfWork.ProductStatusHistory.AddAsync(newHistory);
			}
			// =============================================================
			// NHÁNH 2: SẢN PHẨM ĐANG CHỜ DUYỆT (Dữ liệu nằm trên Redis)
			// =============================================================
			else
			{
				var redisKey = $"ewise:draft_product:{post.PostId}";
				var draftJson = await _redisCacheService.GetStringAsync(redisKey);

				if (!string.IsNullOrEmpty(draftJson))
				{
					// Trick: Đưa JSON vào CheckMessage để hàm Filter/Detail vẫn hiển thị được thông tin bài đã hủy
					if (post.CheckMessage == null) post.CheckMessage = new List<string>();
					post.CheckMessage.Add($"[REJECTED_PRODUCT_DATA]|{draftJson}");

					
				}

				post.Status = PostStatus.DA_HUY.ToString();
				post.RejectMessage = rejectMessage;

				// Xóa Key nháp chính trên Redis
				await _redisCacheService.RemoveAsync(redisKey);
			}

			// 3. CẬP NHẬT CHUNG VÀO DATABASE
			_unitOfWork.Posts.Update(post);
			await _unitOfWork.SaveAsync();

			return true;
		}

		public async Task<PagedResultModel<ProductDetailModel>> GetProductsByPackageIdAsync(string packageId, int page, int limit)
		{
			var (products, totalCount) = await _productRepository.GetPagedProductsByPackageIdAsync(packageId, page, limit);

			var resultList = new List<ProductDetailModel>();

			if (products != null && products.Any())
			{
				foreach (var p in products)
				{
					resultList.Add(new ProductDetailModel
					{
						ProductId = p.ProductId,
						Description = p.Description,
						BrandName = p.Brand?.Name,
						BrandId = p.BrandId,
						CategoryId = p.CategoryId,
						CategoryName = p.Category?.Name,
						QrCode = p.QRCode,
						IsChecked = p.isChecked,
						Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(p.Status),

						Attributes = new List<ProductValueDetailModel>()
					});
				}
			}

			return new PagedResultModel<ProductDetailModel>(resultList, page, limit, totalCount);
		}

		public async Task<List<ProductComeWarehouseDetailModel>> GetProductNeedToPickUp(Guid userId, DateOnly pickUpDate)
		{
			var products = await _productRepository.GetProductsNeedToPickUpAsync(userId, pickUpDate);
			var productDetails = products.Select(product =>
			{
				var post = product.Post;
				return MapToDetailModel(product, post);
			});
			return productDetails.ToList();
		}

		public async Task<bool> SeederQrCodeInProduct(List<Guid> productIds, List<string> QrCode)
		{
			int limit = Math.Min(productIds.Count, QrCode.Count);
			for (int i = 0; i < limit; i++)
			{
				var currentId = productIds[i];
				var currentQr = QrCode[i];

				var product = await _productRepository.GetAsync(p => p.ProductId == currentId);

				if (product != null)
				{
					product.QRCode = currentQr;
					product.Status = ProductStatus.DA_THU_GOM.ToString();
					_unitOfWork.Products.Update(product);
					var route = await _unitOfWork.CollecctionRoutes.GetAsync(r => r.ProductId == currentId);
					if (route == null) throw new AppException("Không tìm thấy tuyến thu gom liên quan đến sản phẩm", 404);
					route.Status = CollectionRouteStatus.HOAN_THANH.ToString();
					route.Actual_Time = TimeOnly.FromDateTime(DateTime.UtcNow);
					_unitOfWork.CollecctionRoutes.Update(route);
				}
			}

			await _unitOfWork.SaveAsync();

			return true;
		}

		public async Task<bool> RemovePackageIdFromProductByQrCode(string qrCode)
		{
			var product = await _productRepository.GetAsync(p => p.QRCode == qrCode);
			if (product == null) throw new AppException("Không tìm thấy sản phẩm", 404);
			product.PackageId = null;
			product.Package = null;
			product.Status = ProductStatus.NHAP_KHO.ToString();
			_unitOfWork.Products.Update(product);
			await _unitOfWork.SaveAsync();
			return true;
		}
		public async Task<bool> RevertProductStatusByQrCodeAndMinusUserPoint(string productQrCode)
		{
			var product = await _unitOfWork.Products.GetAsync(p => p.QRCode == productQrCode);
			if (product == null) throw new AppException("Không tìm thấy sản phẩm với mã QR đã cho", 404);

			var histories = await _unitOfWork.ProductStatusHistory
				.GetsAsync(h => h.ProductId == product.ProductId); 

			var orderedHistories = histories.OrderByDescending(h => h.ChangedAt).ToList();

			if (orderedHistories.Any())
			{
				var currentHistory = orderedHistories.First();
				_unitOfWork.ProductStatusHistory.Delete(currentHistory);
			}

			

			product.Status = ProductStatus.DA_THU_GOM.ToString();
			_unitOfWork.Products.Update(product);

			// 3. Gọi service để thu hồi điểm của User
			await _pointTransactionService.RevertPointFromCollectionPoint(product.ProductId, product.UserId, false);

			// 4. Lưu tất cả thay đổi
			await _unitOfWork.SaveAsync();

			return true;
		}

		public async Task<bool> CheckExistingQRCode(string qrCode)
		{
			var exists = await _productRepository.GetAsync(p => p.QRCode == qrCode);
			return exists != null;
		}
		public async Task<bool> UpdateProductInformation(Guid categoryId, Guid brandId, List<string> images, Guid productId)
		{
			var product = await _unitOfWork.Products.GetAsync(p => p.ProductId == productId);
			if (product == null) throw new AppException("Không tìm thấy sản phẩm với Id đã cho", 404);
			var category = await _unitOfWork.Categories.GetAsync(c => c.CategoryId == categoryId);
			if (category == null) throw new AppException("Không tìm thấy danh mục với Id đã cho", 404);
			var brand = await _unitOfWork.Brands.GetAsync(b => b.BrandId == brandId);
			if (brand == null) throw new AppException("Không tìm thấy thương hiệu với Id đã cho", 404);

			product.CategoryId = categoryId;
			product.BrandId = brandId;
			_unitOfWork.Products.Update(product);
			var existingImages = await _unitOfWork.Images.GetsAsync(pi => pi.ProductId == productId);
			if (existingImages != null && existingImages.Any())
			{
				foreach (var img in existingImages)
				{
					_unitOfWork.Images.Delete(img);
				}
			}

			if (images != null && images.Any())
			{
				foreach (var imageUrl in images)
				{
					var newImage = new Image
					{
						ProductId = productId,
						ImageUrl = imageUrl
					};
					_unitOfWork.Images.Add(newImage);
				}
			}

			var result = await _unitOfWork.SaveAsync();

			return result > 0;
		}
	}
}
