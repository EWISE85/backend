using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class PackageService : IPackageService
	{
		private readonly IProductService _productService;
		private readonly IPackageRepository _packageRepository;
		private readonly IProductStatusHistoryRepository _productStatusHistoryRepository;
		private readonly IUnitOfWork _unitOfWork;
        private readonly CapacityHelper _capacityHelper;
		private readonly IRankService _rankService;
		private readonly IWebNotificationService _notificationService;

		public PackageService(IProductService productService, IPackageRepository packageRepository, IProductStatusHistoryRepository productStatusHistoryRepository, IUnitOfWork unitOfWork, CapacityHelper capacityHelper,IRankService rankService, IWebNotificationService notificationService)
		{
			_productService = productService;
			_packageRepository = packageRepository;
			_productStatusHistoryRepository = productStatusHistoryRepository;
			_unitOfWork = unitOfWork;
            _capacityHelper = capacityHelper;
			_rankService = rankService;
			_notificationService = notificationService;
		}

		public async Task<string> CreatePackageAsync(CreatePackageModel model)
		{
			var newPackage = new Packages
			{
				PackageId = model.PackageId,
                CollectionUnitId = model.SmallCollectionPointsId,
				CreateAt = DateTime.UtcNow,
				Status = PackageStatus.DANG_DONG_GOI.ToString()
			};
			var newPackageStatusHistory = new PackageStatusHistory
			{
				PackageStatusHistoryId = Guid.NewGuid(),
				PackageId = newPackage.PackageId,
				ChangedAt = DateTime.UtcNow,
				StatusDescription = "Gói hàng đang được đóng gói",
				Status = PackageStatus.DANG_DONG_GOI.ToString()
			};

			await _unitOfWork.Packages.AddAsync(newPackage);
			await _unitOfWork.PackageStatusHistory.AddAsync(newPackageStatusHistory);
			foreach (var qrCode in model.ProductsQrCode)
			{
				var product = await _productService.GetByQrCode(qrCode);

				if (product != null)
				{
					await _productService.AddPackageIdToProductByQrCode(product.QrCode, newPackage.PackageId);
					var newHistory = new ProductStatusHistory
					{
						ProductStatusHistoryId = Guid.NewGuid(),
						ProductId = product.ProductId,
						ChangedAt = DateTime.UtcNow,
						StatusDescription = "Sản phẩm đã được đóng gói",
						Status = ProductStatus.DA_DONG_THUNG.ToString()
					};
					await _unitOfWork.ProductStatusHistory.AddAsync(newHistory);

				}
			}
			await _unitOfWork.SaveAsync();
			return newPackage.PackageId;
		}

		public async Task<PackageDetailModel> GetPackageById(string packageId, int page = 1, int limit = 10)
		{
			var package = await _packageRepository.GetAsync(
				p => p.PackageId == packageId,
				includeProperties: "CollectionUnits"
            );

			if (package == null) throw new AppException("Không tìm thấy package", 404);
			var packageStatusHistories = await _unitOfWork.PackageStatusHistory.GetsAsync(p => p.PackageId == packageId);
			if (packageStatusHistories == null)
			{
				packageStatusHistories = new List<PackageStatusHistory>();
			}
			var statusHistoryModels = packageStatusHistories
				.OrderByDescending(h => h.ChangedAt)
				.Select(h => new PackageStatusHistoryModel
				{
					Description = h.StatusDescription,
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PackageStatus>(h.Status),
					CreateAt = h.ChangedAt
				})
				.ToList();
			var pagedProducts = await _productService.GetProductsByPackageIdAsync(packageId, page, limit);

			return new PackageDetailModel
			{
				PackageId = package.PackageId,
				SmallCollectionPointsId = package.CollectionUnitId,
				SmallCollectionPointsName = package.CollectionUnits?.Name,
				SmallCollectionPointsAddress = package.CollectionUnits?.Address,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PackageStatus>(package.Status),
				StatusHistories = statusHistoryModels,
				Products = pagedProducts 
			};
		}

		public async Task<PagedResultModel<PackageDetailModel>> GetPackagesByCompanyQuery(PackageSearchCompanyQueryModel query)
		{
			string? statusEnum = null;
			if (!string.IsNullOrEmpty(query.Status))
			{
				var statusValue = StatusEnumHelper.GetValueFromDescription<PackageStatus>(query.Status);
				statusEnum = statusValue.ToString();
			}

			var (pagedPackages, totalCount) = await _packageRepository.GetPagedPackagesWithDetailsByCompanyAsync(
				query.CompanyId,
				query.FromDate, 
				query.ToDate,   
				statusEnum,
				query.Page,
				query.Limit
			);

			// 3. Map dữ liệu từ Entity sang Model trả về
			var resultItems = pagedPackages.Select(pkg =>
			{
				int totalProds = pkg.Products?.Count ?? 0;

				var summaryProducts = new PagedResultModel<ProductDetailModel>(
					new List<ProductDetailModel>(),
					1,
					0,
					totalProds
				);

				return new PackageDetailModel
				{
					PackageId = pkg.PackageId,
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PackageStatus>(pkg.Status),
					SmallCollectionPointsId = pkg.CollectionUnitId,
					SmallCollectionPointsName = pkg.CollectionUnits?.Name,
					Products = summaryProducts
				};
			}).ToList();

			return new PagedResultModel<PackageDetailModel>(resultItems, query.Page, query.Limit, totalCount);
		}

		public async Task<PagedResultModel<PackageDetailModel>> GetPackagesByQuery(PackageSearchQueryModel query)
		{
			string? statusEnum = null;
			if (!string.IsNullOrEmpty(query.Status))
			{
				var statusValue = StatusEnumHelper.GetValueFromDescription<PackageStatus>(query.Status);
				statusEnum = statusValue.ToString();
			}

			
			var (pagedPackages, totalCount) = await _packageRepository.GetPagedPackagesWithDetailsAsync(
				query.SmallCollectionPointsId,
				statusEnum,
				query.Page,
				query.Limit
			);

			var resultItems = pagedPackages.Select(pkg =>
			{
				int totalProductsInPkg = pkg.Products?.Count ?? 0;

				var summaryProducts = new PagedResultModel<ProductDetailModel>(
					new List<ProductDetailModel>(), 
					1,                             
					0,                             
					totalProductsInPkg             
				);

				return new PackageDetailModel
				{
					PackageId = pkg.PackageId,
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PackageStatus>(pkg.Status),
					SmallCollectionPointsId = pkg.CollectionUnitId,
					SmallCollectionPointsName = pkg.CollectionUnits?.Name,
					SmallCollectionPointsAddress = pkg.CollectionUnits?.Address,

					// Gán object tóm tắt vào đây
					Products = summaryProducts
				};
			}).ToList();

			// 4. Trả về kết quả phân trang cho danh sách Package
			return new PagedResultModel<PackageDetailModel>(resultItems, query.Page, query.Limit, totalCount);
		}


		public async Task<PagedResultModel<PackageDetailModel>> GetPackagesByRecylerQuery(PackageRecyclerSearchQueryModel query)
		{
			string? statusEnum = null;
			if (!string.IsNullOrEmpty(query.Status))
			{
				var statusValue = StatusEnumHelper.GetValueFromDescription<PackageStatus>(query.Status);
				statusEnum = statusValue.ToString();
			}

			var (pagedPackages, totalCount) = await _packageRepository.GetPagedPackagesWithDetailsByRecyclerAsync(
				query.RecyclerCompanyId,
				statusEnum,
				query.Page,
				query.Limit
			);

			var resultItems = pagedPackages.Select(pkg =>
			{


				int totalProds = pkg.Products?.Count ?? 0;

				var summaryProducts = new PagedResultModel<ProductDetailModel>(
					new List<ProductDetailModel>(), 
					1,                             
					0,                             
					totalProds                      
				);

				return new PackageDetailModel
				{
					PackageId = pkg.PackageId,
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PackageStatus>(pkg.Status),
					SmallCollectionPointsId = pkg.CollectionUnitId,
					SmallCollectionPointsName = pkg.CollectionUnits?.Name,
					Products = summaryProducts
				};
			}).ToList();

			// 4. Trả về danh sách Package phân trang
			return new PagedResultModel<PackageDetailModel>(resultItems, query.Page, query.Limit, totalCount);
		}

		public async Task<List<PackageDetailModel>> GetPackagesWhenDelivery()
		{
			
			var deliveringPackages = await _packageRepository.GetsAsync(
				filter: p => p.Status == PackageStatus.DANG_VAN_CHUYEN.ToString(),
				includeProperties: "Products,CollectionUnits"
            );

			var result = new List<PackageDetailModel>();

			if (deliveringPackages != null && deliveringPackages.Any())
			{
				result = deliveringPackages.Select(pkg =>
				{
					int totalCount = pkg.Products?.Count ?? 0;

					var summaryProducts = new PagedResultModel<ProductDetailModel>(
						new List<ProductDetailModel>(), // Data rỗng
						1, 0, totalCount
					);

					return new PackageDetailModel
					{
						PackageId = pkg.PackageId,
						Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PackageStatus>(pkg.Status),

						SmallCollectionPointsId = pkg.CollectionUnitId,
						SmallCollectionPointsName = pkg.CollectionUnits?.Name,     
						SmallCollectionPointsAddress = pkg.CollectionUnits?.Address,

						Products = summaryProducts
					};
				}).ToList();
			}

			return result;
		}

		public async Task<bool> UpdatePackageAsync(UpdatePackageModel model)
		{
			// 1. Lấy Package và kiểm tra tồn tại
			var package = await _unitOfWork.Packages.GetAsync(p => p.PackageId == model.PackageId);
			if (package == null) throw new AppException("Không tìm thấy package", 404);

			package.CollectionUnitId = model.SmallCollectionPointsId;

			// 2. Lấy tất cả sản phẩm liên quan (Đang trong thùng OR nằm trong list QR mới)
			var qrCodesList = model.ProductsQrCode.ToList();
			var relevantProducts = await _unitOfWork.Products.GetsAsync(
				p => p.PackageId == model.PackageId || qrCodesList.Contains(p.QRCode));

			var newQrCodesSet = model.ProductsQrCode.ToHashSet();

			foreach (var product in relevantProducts)
			{
				// TRƯỜNG HỢP 1: Bị loại bỏ khỏi thùng (Có trong thùng cũ nhưng không có trong list mới)
				if (product.PackageId == model.PackageId && !newQrCodesSet.Contains(product.QRCode))
				{
					product.PackageId = null;
					product.Package = null;
					product.Status = ProductStatus.NHAP_KHO.ToString();

					// Xóa lịch sử cũ nếu bạn muốn (theo logic cũ của bạn) hoặc giữ lại để track
					var oldHistory = await _unitOfWork.ProductStatusHistory.GetAsync(
						h => h.ProductId == product.ProductId && h.Status == ProductStatus.DA_DONG_THUNG.ToString());

					if (oldHistory != null)
					{
						_unitOfWork.ProductStatusHistory.Delete(oldHistory);
					}
				}

				// TRƯỜNG HỢP 2: Được thêm mới vào thùng hoặc giữ lại trong thùng
				else if (newQrCodesSet.Contains(product.QRCode))
				{
					// Chỉ tạo lịch sử nếu trạng thái thực sự thay đổi sang DA_DONG_THUNG
					if (product.Status != ProductStatus.DA_DONG_THUNG.ToString())
					{
						product.Status = ProductStatus.DA_DONG_THUNG.ToString();

						// TẠO LỊCH SỬ MỚI
						var history = new ProductStatusHistory
						{
							ProductStatusHistoryId = Guid.NewGuid(), 
							ProductId = product.ProductId,
							Status = ProductStatus.DA_DONG_THUNG.ToString(),
							ChangedAt = DateTime.UtcNow, 
							StatusDescription = $"Được thêm vào kiện hàng {package.PackageId}"
						};

						await _unitOfWork.ProductStatusHistory.AddAsync(history);
					}

					// Cập nhật PackageId (trường hợp đổi từ thùng này sang thùng khác hoặc từ ngoài vào)
					product.PackageId = package.PackageId;
				}
			}

			// 3. Lưu tất cả thay đổi (Package, Product, History) trong 1 Transaction duy nhất
			await _unitOfWork.SaveAsync();

			return true;
		}

		public async Task<bool> UpdatePackageStatus(string packageId, string status)
		{
			var package = await _unitOfWork.Packages.GetAsync(
					p => p.PackageId == packageId,
					includeProperties: "CollectionUnits" // Để lấy được CompanyId
				);
			if (package == null) throw new AppException("Không tìm thấy package", 404);
			var products = await _productService.GetProductsByPackageIdAsync(packageId);
			if (products.Count == 0 ) throw new AppException("Package không có sản phẩm nào", 400);
			
			var statusEnum = StatusEnumHelper.GetValueFromDescription<PackageStatus>(status);
			package.Status = statusEnum.ToString();
			var newPackageStatusHistory = new PackageStatusHistory
			{
				PackageStatusHistoryId = Guid.NewGuid(),
				PackageId = package.PackageId,
				ChangedAt = DateTime.UtcNow,
				StatusDescription = status == "Đã đóng thùng" ? "Kiện hàng đã được đóng gói hoàn tất" : "Chưa rõ",
				Status = statusEnum.ToString()
			};

			_unitOfWork.Packages.Update(package);
			await _unitOfWork.PackageStatusHistory.AddAsync(newPackageStatusHistory);
			await _unitOfWork.SaveAsync();
			if (statusEnum == PackageStatus.DA_DONG_THUNG)
			{
				await SendNotifyToCompanyAdmin(package);
			}
			return true;
		}
		private async Task SendNotifyToCompanyAdmin(Packages package)
		{
			// Tìm các Admin thuộc Công ty của cái kho này
			// Giả sử Role của Admin công ty là "AdminCompany" (bạn check lại enum UserRole của mình)
			var companyId = package.CollectionUnits.CompanyId;

			var adminCompanies = await _unitOfWork.Users.GetAllAsync(u =>
				u.CompanyId == companyId &&
				u.Role == UserRole.RecyclingCompany.ToString() // Thay bằng UserRole.AdminCompany.ToString() nếu có enum
			);

			foreach (var admin in adminCompanies)
			{
				await _notificationService.SendNotificationAsync(
					userId: admin.UserId.ToString(),
					title: "Kiện hàng đã sẵn sàng",
					message: $"Kiện hàng {package.PackageId} tại kho {package.CollectionUnits.Name} đã đóng thùng. Vui lòng sắp xếp lấy hàng.",
					type: "success",
					data: new
					{
						PackageId = package.PackageId,
						CollectionUnitName = package.CollectionUnits.Name,
						Action = "PACKAGE_CLOSED"
					}
				);

				var notificationDb = new Notifications
				{
					NotificationId = Guid.NewGuid(),
					UserId = admin.UserId,
					Title = "Kiện hàng đã sẵn sàng",
					Body = $"Kiện hàng {package.PackageId} tại kho {package.CollectionUnits.Name} đã đóng thùng.",
					Type = "System",
					CreatedAt = DateTime.UtcNow,
					IsRead = false
				};
				await _unitOfWork.Notifications.AddAsync(notificationDb);
			}
			await _unitOfWork.SaveAsync();
		}
		public async Task<bool> UpdatePackageStatusDelivery(string deliveryQrCode, List<string> packageIds, string status)
		{
			if (packageIds == null || !packageIds.Any()) return false;

			var packages = await _unitOfWork.Packages.GetAllAsync(
		p => packageIds.Contains(p.PackageId),
		includeProperties: "CollectionUnits"
	);

			var pointIdsToSync = packages.Select(p => p.CollectionUnitId).Distinct().ToList();

			var statusEnum = StatusEnumHelper.GetValueFromDescription<PackageStatus>(status);
			var productStatusEnum = statusEnum == PackageStatus.DANG_VAN_CHUYEN ? ProductStatus.DANG_VAN_CHUYEN : ProductStatus.TAI_CHE;

			string historyDescription = statusEnum == PackageStatus.DANG_VAN_CHUYEN ? "Sản phẩm đang được vận chuyển" : "Sản phẩm đã được tái chế";
			string statusString = statusEnum.ToString();
			string productStatusString = productStatusEnum.ToString();

			bool hasAnySuccess = false;
			var handoverTime = DateTime.UtcNow;

			foreach (var pkgId in packageIds)
			{
				var package = packages.FirstOrDefault(p => p.PackageId == pkgId);
				if (package == null) continue;

				package.Status = statusString;
				package.DeliveryQrCode = deliveryQrCode;
				package.DeliveryHandoverAt = handoverTime;

				var newPackageStatusHistory = new PackageStatusHistory
				{
					PackageStatusHistoryId = Guid.NewGuid(),
					PackageId = package.PackageId,
					ChangedAt = DateTime.UtcNow,
					StatusDescription = "Kiện hàng đang được vận chuyển về công ty tái chế",
					Status = statusString
				};

				_unitOfWork.Packages.Update(package);
				await _unitOfWork.PackageStatusHistory.AddAsync(newPackageStatusHistory);

				var productList = await _productService.GetProductsByPackageIdAsync(pkgId);
				if (productList != null && productList.Any())
				{
					foreach (var product in productList)
					{
						await _productService.UpdateProductStatusByQrCode(product.QrCode, productStatusString);

						var newHistory = new ProductStatusHistory
						{
							ProductStatusHistoryId = Guid.NewGuid(),
							ProductId = product.ProductId,
							ChangedAt = DateTime.UtcNow,
							StatusDescription = historyDescription,
							Status = productStatusString
						};
						await _unitOfWork.ProductStatusHistory.AddAsync(newHistory);
					}
				}
				hasAnySuccess = true;
			}

			if (!hasAnySuccess) return false;

			await _unitOfWork.SaveAsync();

			foreach (var pointId in pointIdsToSync)
			{
				if (!string.IsNullOrEmpty(pointId))
					await _capacityHelper.SyncRealtimeCapacityAsync(pointId);
			}
			if (statusEnum == PackageStatus.DANG_VAN_CHUYEN)
			{
				await SendNotifyOnDelivery(packages.ToList());
			}
			return true;
		}
		private async Task SendNotifyOnDelivery(List<Packages> packages)
		{
			var groupedByCompany = packages
				.Where(p => p.CollectionUnits != null)
				.GroupBy(p => p.CollectionUnits.CompanyId);

			foreach (var group in groupedByCompany)
			{
				var companyId = group.Key;
				var packageCount = group.Count();
				var packageListIds = group.Select(p => p.PackageId).ToList();

				// Tìm các Admin thuộc Công ty tái chế này
				var admins = await _unitOfWork.Users.GetAllAsync(u =>
					u.CompanyId == companyId &&
					u.Role == UserRole.RecyclingCompany.ToString()
				);

				foreach (var admin in admins)
				{
					string title = "Đơn hàng đang đến";
					string message = $"{packageCount} kiện hàng đã được giao cho bên vận chuyển và đang trên đường vận chuyển đến bạn.";

					// 1. Gửi SignalR
					await _notificationService.SendNotificationAsync(
						userId: admin.UserId.ToString(),
						title: title,
						message: message,
						type: "success",
						data: new
						{
							PackageIds = packageListIds,
							Action = "PACKAGES_IN_TRANSIT",
							Timestamp = DateTime.UtcNow
						}
					);

					// 2. Lưu vào Database
					var notificationDb = new Notifications
					{
						NotificationId = Guid.NewGuid(),
						UserId = admin.UserId,
						Title = title,
						Body = message,
						Type = "System",
						CreatedAt = DateTime.UtcNow,
						IsRead = false
					};
					await _unitOfWork.Notifications.AddAsync(notificationDb);
				}
			}
			await _unitOfWork.SaveAsync();
		}
		public async Task<bool> UpdatePackageStatusRecycler(string packageId, string status)
		{
			var package = await _packageRepository.GetAsync(p => p.PackageId == packageId);

			var productList = await _productService.GetProductsByPackageIdAsync(packageId);

			if (package == null)
			{
				return false;
			}

			var statusEnum = StatusEnumHelper.GetValueFromDescription<PackageStatus>(status);

			var productStatusEnum = statusEnum;

			package.Status = statusEnum.ToString();
			var newPackageStatusHistory = new PackageStatusHistory
			{
				PackageStatusHistoryId = Guid.NewGuid(),
				PackageId = package.PackageId,
				ChangedAt = DateTime.UtcNow,
				StatusDescription = "Kiện hàng đã về tới công ty tái chế",
				Status = statusEnum.ToString()
			};
			await _unitOfWork.PackageStatusHistory.AddAsync(newPackageStatusHistory);
			foreach (var product in productList)
			{
				await _productService.UpdateProductStatusByQrCode(product.QrCode, statusEnum.ToString());

				var newHistory = new ProductStatusHistory
				{
					ProductStatusHistoryId = Guid.NewGuid(),

					ProductId = product.ProductId,

					ChangedAt = DateTime.UtcNow,

					StatusDescription = "Sản phẩm đã được tái chế",

					Status = productStatusEnum.ToString()
				};

				await _unitOfWork.ProductStatusHistory.AddAsync(newHistory);

			}

			_unitOfWork.Packages.Update(package);

			await _unitOfWork.SaveAsync();

			return true;

		}

		//public async Task<bool> UpdatePackageStatusRecycler(string packageId, string status)
		//{
		//    var package = await _unitOfWork.Packages.GetAsync(p => p.PackageId == packageId);
		//    if (package == null) return false;

		//    var productList = await _unitOfWork.Products.GetAllAsync(
		//        p => p.PackageId == packageId,
		//        includeProperties: "Category,Category.ParentCategory,ProductValues.Attribute"
		//    );

		//    var statusEnum = StatusEnumHelper.GetValueFromDescription<PackageStatus>(status);
		//    string statusString = statusEnum.ToString();

		//    var userCo2Sum = new Dictionary<Guid, double>();

		//    foreach (var product in productList)
		//    {
		//        product.Status = statusString;
		//        _unitOfWork.Products.Update(product);

		//        if (statusEnum == PackageStatus.TAI_CHE)
		//        {
		//            double actualWeight = 0;
		//            if (product.ProductValues != null && product.ProductValues.Any())
		//            {
		//                var weightAttr = product.ProductValues.FirstOrDefault(v =>
		//                    v.Attribute != null && v.Attribute.Name.Contains("Trọng lượng", StringComparison.OrdinalIgnoreCase));

		//                if (weightAttr != null && weightAttr.Value.HasValue)
		//                    actualWeight = weightAttr.Value.Value;
		//            }

		//            if (actualWeight <= 0)
		//                actualWeight = product.Category.DefaultWeight > 0 ? product.Category.DefaultWeight : 1.0;

		//            double factor = product.Category.ParentCategory?.EmissionFactor ?? product.Category.EmissionFactor;
		//            if (factor <= 0) factor = 0.5;

		//            double co2OfThisProduct = actualWeight * factor;

		//            if (userCo2Sum.ContainsKey(product.UserId))
		//                userCo2Sum[product.UserId] += co2OfThisProduct;
		//            else
		//                userCo2Sum[product.UserId] = co2OfThisProduct;
		//        }

		//        await _unitOfWork.ProductStatusHistory.AddAsync(new ProductStatusHistory
		//        {
		//            ProductStatusHistoryId = Guid.NewGuid(),
		//            ProductId = product.ProductId,
		//            ChangedAt = DateTime.UtcNow,
		//            Status = statusString,
		//            StatusDescription = "Sản phẩm đã được tái chế"
		//        });
		//    }

		//    foreach (var entry in userCo2Sum)
		//    {
		//        var user = await _unitOfWork.Users.GetByIdAsync(entry.Key);
		//        if (user != null)
		//        {
		//            user.TotalCo2Saved += entry.Value;

		//            var allRanks = await _unitOfWork.Ranks.GetAllAsync();
		//            var applicableRank = allRanks
		//                .Where(r => r.MinCo2 <= user.TotalCo2Saved)
		//                .OrderByDescending(r => r.MinCo2)
		//                .FirstOrDefault();

		//            if (applicableRank != null) user.CurrentRankId = applicableRank.RankId;

		//            _unitOfWork.Users.Update(user);
		//        }
		//    }

		//    package.Status = statusString;
		//    _unitOfWork.Packages.Update(package);

		//    await _unitOfWork.PackageStatusHistory.AddAsync(new PackageStatusHistory
		//    {
		//        PackageStatusHistoryId = Guid.NewGuid(),
		//        PackageId = package.PackageId,
		//        ChangedAt = DateTime.UtcNow,
		//        Status = statusString,
		//        StatusDescription = "Kiện hàng đã về tới công ty tái chế"
		//    });

		//    await _unitOfWork.SaveAsync();
		//    return true;
		//}

		public async Task<PagedResultModel<PackageDetailModel>> GetPackagesByDeliveryQrCodeAsync(string deliveryQrCode, int page, int limit)
		{
			var (pagedPackages, totalCount) = await _packageRepository.GetPagedPackagesByDeliveryQrCodeAsync(
				deliveryQrCode,
				page,
				limit
			);

			var resultItems = pagedPackages.Select(pkg =>
			{
				int totalProductsInPkg = pkg.Products?.Count ?? 0;

				var summaryProducts = new PagedResultModel<ProductDetailModel>(
					new List<ProductDetailModel>(),
					1,
					0,
					totalProductsInPkg
				);

				var histories = pkg.PackageStatusHistories?.Select(h => new PackageStatusHistoryModel
				{
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PackageStatus>(h.Status),
					Description = h.StatusDescription,
					CreateAt = h.ChangedAt
				}).OrderByDescending(h => h.CreateAt).ToList() ?? new List<PackageStatusHistoryModel>();

				return new PackageDetailModel
				{
					PackageId = pkg.PackageId,
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PackageStatus>(pkg.Status),
					SmallCollectionPointsId = pkg.CollectionUnitId,
					SmallCollectionPointsName = pkg.CollectionUnits?.Name,
					SmallCollectionPointsAddress = pkg.CollectionUnits?.Address,
					RecyclerName = pkg.CollectionUnits?.Company?.Name,
					RecyclerAddress = pkg.CollectionUnits?.Company?.Address,
					StatusHistories = histories,
					Products = summaryProducts
				};
			}).ToList();

			return new PagedResultModel<PackageDetailModel>(resultItems, page, limit, totalCount);
		}

		public async Task<PagedResultModel<PackageDetailModel>> GetTrackingPackage(string? recyclerId, DateOnly? fromDate, DateOnly? toDate, string smallCollectionPointId ,string? packageId, string? status, int page, int limit)
		{
			var statusEnum = string.IsNullOrEmpty(status) ? (string?)null : StatusEnumHelper.GetValueFromDescription<PackageStatus>(status).ToString();
			var (pagedPackages, totalCount) = await _packageRepository.GetTrackingPackage(
				recyclerId,
				fromDate, toDate,
				smallCollectionPointId,	
				packageId,
				statusEnum,
				page,
				limit
			);
			var resultItems = pagedPackages.Select(pkg =>
			{
				int totalProductsInPkg = pkg.Products?.Count ?? 0;

				var summaryProducts = new PagedResultModel<ProductDetailModel>(
					new List<ProductDetailModel>(),
					1,
					0,
					totalProductsInPkg
				);

				var histories = pkg.PackageStatusHistories?.Select(h => new PackageStatusHistoryModel
				{
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PackageStatus>(h.Status),
					Description = h.StatusDescription,
					CreateAt = h.ChangedAt
				}).OrderByDescending(h => h.CreateAt).ToList() ?? new List<PackageStatusHistoryModel>();

				return new PackageDetailModel
				{
					PackageId = pkg.PackageId,
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PackageStatus>(pkg.Status),
					SmallCollectionPointsId = pkg.CollectionUnitId,
					SmallCollectionPointsName = pkg.CollectionUnits?.Name,
					SmallCollectionPointsAddress = pkg.CollectionUnits?.Address,
					RecyclerName = pkg.CollectionUnits?.Company?.Name,
					RecyclerAddress = pkg.CollectionUnits?.Company?.Address,
					StatusHistories = histories,
					DeliveryAt = pkg.DeliveryHandoverAt,
					Products = summaryProducts
				};
			}).ToList();

			return new PagedResultModel<PackageDetailModel>(resultItems, page, limit, totalCount);
		}

		public async Task<bool> CheckExistingPackageQR(string packageId)
		{
			var exists = await _packageRepository.GetAsync(p => p.PackageId == packageId);
			if (exists != null)
			{
				throw new AppException($"Mã kiện hàng {packageId} đã tồn tại. Vui lòng sử dụng mã khác.", 400);
			}
			return true;
		}
	}
}
