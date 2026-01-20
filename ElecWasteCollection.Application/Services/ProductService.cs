using DocumentFormat.OpenXml.Spreadsheet;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
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
		public ProductService(IProductRepository productRepository, IUnitOfWork unitOfWork, IProductImageRepository productImageRepository, IPointTransactionService pointTransactionService, IBrandRepository brandRepository, ICategoryRepository categoryRepository, IProductStatusHistoryRepository productStatusHistoryRepository, IAttributeOptionRepository attributeOptionRepository, IPackageRepository packageRepository)
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
		}



		public async Task<bool> AddPackageIdToProductByQrCode(string qrCode, string? packageId)
		{
			var product = await _productRepository.GetAsync(p => p.QRCode == qrCode);
			if (product == null) throw new AppException("Không tìm thấy sản phẩm",404);
			product.PackageId = packageId;
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
				SmallCollectionPointId = createProductRequest.SmallCollectionPointId,
				isChecked = false,
				Status = ProductStatus.NHAP_KHO.ToString()
			};
			await _unitOfWork.Products.AddAsync(newProduct);

			var productImages = new List<ProductImages>();
			for (int i = 0; i < createProductRequest.Images.Count; i++)
			{
				var newProductImage = new ProductImages
				{
					ImageUrl = createProductRequest.Images[i],
					ProductId = newProduct.ProductId,
					ProductImagesId = Guid.NewGuid()
				};
				productImages.Add(newProductImage);
				await _unitOfWork.ProductImages.AddAsync(newProductImage);
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

			var post = product.Posts?.FirstOrDefault();

			
			var imageUrls = product.ProductImages?.Select(img => img.ImageUrl).ToList() ?? new List<string>();
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
				AttributeId = pv.AttributeId.Value,
				AttributeName = pv.Attribute?.Name,
				OptionId = pv.AttributeOptionId,
				Value = pv.Value.ToString(),
				OptionName = option?.OptionName, 
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
				var post = product.Posts?.FirstOrDefault();
				return MapToDetailModel(product, post);
			})
			.Where(x => x != null)
			.OrderByDescending(x => x.Status)
			.ToList();
			return combinedList;
		}




		private ProductComeWarehouseDetailModel MapToDetailModel(Products product, Post? post)
		{
			if (product == null) throw new AppException("Không tìm thấy product", 404);

			var imageUrls = product.ProductImages?
				.Select(img => img.ImageUrl)
				.ToList() ?? new List<string>();

			double? realPoint = product.PointTransactions?
				.FirstOrDefault()?.Point;

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
				RealPoint = realPoint,
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

		public async Task<bool> UpdateProductStatusByQrCodeAndPlusUserPoint(string productQrCode, string status, UserReceivePointFromCollectionPointModel model)
		{
			var product = await _unitOfWork.Products.GetAsync(p => p.QRCode == productQrCode); 
			if (product == null) throw new AppException("Không tìm thấy sản phẩm với mã QR đã cho", 404);

			var description = "";
			if(model.Description != null)
			{
				description = model.Description;
			}
			else
			{
				description = "Sản phầm đã về đến kho";
			}
			var pointTransaction = new CreatePointTransactionModel
			{
				UserId = product.UserId,
				ProductId = product.ProductId,
				Point = model.Point,
				Desciption = description,
			};
			var statusEnum = StatusEnumHelper.GetValueFromDescription<ProductStatus>(status);
			product.Status = statusEnum.ToString();
			_unitOfWork.Products.Update(product);
			var newHistory = new ProductStatusHistory
			{
				ProductStatusHistoryId = Guid.NewGuid(),
				ProductId = product.ProductId,
				ChangedAt = DateTime.UtcNow,
				StatusDescription = "Sản phẩm đã về đến kho",
				Status = statusEnum.ToString()
			};
			await _unitOfWork.ProductStatusHistory.AddAsync(newHistory); 
			await _pointTransactionService.ReceivePointFromCollectionPoint(pointTransaction,false);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<List<ProductComeWarehouseDetailModel>> GetAllProductsByUserId(Guid userId)
		{
			var products = await _productRepository.GetProductsBySenderIdWithDetailsAsync(userId);

			if (products == null || !products.Any())
			{
				return new List<ProductComeWarehouseDetailModel>();
			}

			var productDetails = products.Select(product =>
			{
				var post = product.Posts?
					.FirstOrDefault(p => p.SenderId == userId);
				return MapToDetailModel(product, post);
			})
			.Where(x => x != null)
			.ToList();
			return productDetails;
		}

		public async Task<ProductDetail?> GetProductDetailByIdAsync(Guid productId)
		{
			var product = await _productRepository.GetProductDetailWithAllRelationsAsync(productId);
			if (product == null) return null;


			var post = product.Posts?.FirstOrDefault();
			//if (post == null) return null;

			List<ProductValueDetailModel> productAttributes = new List<ProductValueDetailModel>();
			if (product.ProductValues != null)
			{
				foreach (var pv in product.ProductValues)
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
					productAttributes.Add(detail);
				}
			}

			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			List<DailyTimeSlots> schedule = new List<DailyTimeSlots>();
			if(post != null)
			{
				if (!string.IsNullOrEmpty(post.ScheduleJson))
				{
					try { schedule = JsonSerializer.Deserialize<List<DailyTimeSlots>>(post.ScheduleJson, options) ?? new List<DailyTimeSlots>(); }
					catch (JsonException) { schedule = new List<DailyTimeSlots>(); }
				}
			}

			var route = product.CollectionRoutes?.FirstOrDefault();
			var shifts = route?.CollectionGroup?.Shifts;
			var senderId = product.UserId;
			var collector = shifts?.Collector;
			var realPoint = product.PointTransactions?.FirstOrDefault()?.Point;
			var sender = await _unitOfWork.Users.GetAsync(u => u.UserId == senderId);
			if (sender == null) throw new AppException("Không tìm thấy người gửi", 404);

			var userResponse = new UserResponse
			{
				UserId = sender?.UserId ?? Guid.Empty,
				Name = sender?.Name,
				Phone = sender?.Phone,
				Email = sender?.Email,
				Avatar = sender?.Avatar,
				Role = sender.Role,
				SmallCollectionPointId = sender?.SmallCollectionPointId
			};

			return new ProductDetail
			{
				ProductId = product.ProductId,
				CategoryId = product.CategoryId,
				CategoryName = product.Category?.Name ?? "Không rõ",
				BrandId = product.BrandId,
				BrandName = product.Brand?.Name ?? "Không rõ", 
				Description = product.Description,
				ProductImages = product.ProductImages?.Select(pi => pi.ImageUrl).ToList() ?? new List<string>(),
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(product.Status),
				EstimatePoint = post?.EstimatePoint,
				Sender = userResponse,
				Address = post?.Address ?? "Không có địa chỉ",
				Schedule = schedule,
				Attributes = productAttributes,
				RejectMessage = post?.RejectMessage ?? "Không có lý do",
				QRCode = product.QRCode,
				IsChecked = product.isChecked,
				RealPoints = realPoint,

				Collector = collector != null ? new CollectorResponse { 
					CollectorId = collector.UserId,
					Name = collector.Name 
				} : null,
				PickUpDate = route?.CollectionDate,
				EstimatedTime = route?.EstimatedTime,
				CollectionRouterId = route?.CollectionRouteId
			};
		}

		private async Task<ProductValueDetailModel> MapProductValueDetailWithOptionAsync(ProductValues pv)
		{
			var option = await _attributeOptionRepository.GetAsync(
				ao => ao.OptionId == pv.AttributeOptionId.Value
			);

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
				
				var post = product.Posts?.FirstOrDefault();
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
					SmallCollectionPointId = sender?.SmallCollectionPointId
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
					ProductImages = product.ProductImages?.Select(pi => pi.ImageUrl).ToList() ?? new List<string>(),
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

		public async Task<bool> CancelProduct(Guid productId, string rejectMessage)
		{
			var product = await _productRepository.GetAsync(p => p.ProductId == productId);
			if (product == null) throw new AppException("Không tìm thấy sản phẩm với Id đã cho", 404);
			var post = await _unitOfWork.Posts.GetAsync(p => p.ProductId == productId);
			if (post == null) throw new AppException("Không tìm thấy bài đăng liên quan đến sản phẩm", 404);
			post.RejectMessage = rejectMessage;
			post.Status = PostStatus.DA_HUY.ToString();
			product.Status = ProductStatus.DA_HUY.ToString();
			var newHistory = new ProductStatusHistory
			{
				ProductStatusHistoryId = Guid.NewGuid(),
				ProductId = product.ProductId,
				ChangedAt = DateTime.UtcNow,
				StatusDescription = "Sản phẩm đã hủy: " + rejectMessage,
				Status = ProductStatus.DA_HUY.ToString()
			};
			_unitOfWork.Posts.Update(post);
			_unitOfWork.Products.Update(product);
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
	}
}
