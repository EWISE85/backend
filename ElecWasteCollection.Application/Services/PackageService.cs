using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
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

		public PackageService(IProductService productService, IPackageRepository packageRepository, IProductStatusHistoryRepository productStatusHistoryRepository, IUnitOfWork unitOfWork)
		{
			_productService = productService;
			_packageRepository = packageRepository;
			_productStatusHistoryRepository = productStatusHistoryRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<string> CreatePackageAsync(CreatePackageModel model)
		{
			var newPackage = new Packages
			{
				PackageId = model.PackageId,
				SmallCollectionPointsId = model.SmallCollectionPointsId,
				CreateAt = DateTime.UtcNow,
				Status = PackageStatus.DANG_DONG_GOI.ToString()
			};
			await _unitOfWork.Packages.AddAsync(newPackage);
			foreach (var qrCode in model.ProductsQrCode)
			{
				var product = await _productService.GetByQrCode(qrCode);

				if (product != null)
				{
					await _productService.AddPackageIdToProductByQrCode(product.QrCode, newPackage.PackageId);
					await _productService.UpdateProductStatusByQrCode(product.QrCode, ProductStatus.DA_DONG_THUNG.ToString());
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

		public async Task<PackageDetailModel> GetPackageById(string packageId)
		{
			var package = await _packageRepository.GetAsync(p => p.PackageId == packageId);
			if (package == null) throw new AppException("Không tìm thấy package", 404);

			var productDetails = await _productService.GetProductsByPackageIdAsync(packageId);

			var packageDetail = new PackageDetailModel
			{
				PackageId = package.PackageId,
				SmallCollectionPointsId = package.SmallCollectionPointsId,
				Status = package.Status,
				Products = productDetails
			};

			return packageDetail;
		}


		public async Task<PagedResult<PackageDetailModel>> GetPackagesByQuery(PackageSearchQueryModel query)
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
				var productDetails = pkg.Products?.Select(product => new ProductDetailModel
				{
					ProductId = product.ProductId,
					Description = product.Description,
					BrandId = product.BrandId,
					BrandName = product.Brand?.Name, 
					CategoryId = product.CategoryId,
					CategoryName = product.Category?.Name, 
					QrCode = product.QRCode,
					IsChecked = product.isChecked,
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(product.Status)
				}).ToList() ?? new List<ProductDetailModel>();

				return new PackageDetailModel
				{
					PackageId = pkg.PackageId,
					Status = pkg.Status,
					SmallCollectionPointsId = pkg.SmallCollectionPointsId,
					Products = productDetails
				};
			}).ToList();

			return new PagedResult<PackageDetailModel>
			{
				Data = resultItems,
				TotalItems = totalCount,
				Page = query.Page,
				Limit = query.Limit,
			};
		}


		public async Task<PagedResult<PackageDetailModel>> GetPackagesByRecylerQuery(PackageRecyclerSearchQueryModel query)
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
				var productDetails = pkg.Products?.Select(product => new ProductDetailModel
				{
					ProductId = product.ProductId,
					Description = product.Description,
					BrandId = product.BrandId,
					BrandName = product.Brand?.Name,
					CategoryId = product.CategoryId,
					CategoryName = product.Category?.Name,
					QrCode = product.QRCode,
					IsChecked = product.isChecked,
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(product.Status)
				}).ToList() ?? new List<ProductDetailModel>();

				return new PackageDetailModel
				{
					PackageId = pkg.PackageId,
					Status = pkg.Status,
					SmallCollectionPointsId = pkg.SmallCollectionPointsId,
					Products = productDetails
				};
			}).ToList();

			return new PagedResult<PackageDetailModel>
			{
				Data = resultItems,
				TotalItems = totalCount,
				Page = query.Page,
				Limit = query.Limit,
			};
		}

		public async Task<List<PackageDetailModel>> GetPackagesWhenDelivery()
		{
			var deliveringPackages = await _packageRepository.GetsAsync(p => p.Status == PackageStatus.DANG_VAN_CHUYEN.ToString());
				

			var result = new List<PackageDetailModel>();

			foreach (var pkg in deliveringPackages)
			{
				var productDetails = await _productService.GetProductsByPackageIdAsync(pkg.PackageId);

				var model = new PackageDetailModel
				{
					PackageId = pkg.PackageId,
					Status = pkg.Status,
					SmallCollectionPointsId = pkg.SmallCollectionPointsId,
					Products = productDetails
				};

				result.Add(model);
			}

			return result;
		}

		public async Task<bool> UpdatePackageAsync(UpdatePackageModel model)
		{
			var package = await _packageRepository.GetAsync(p => p.PackageId == model.PackageId);
			if (package == null) throw new AppException("Không tìm thấy package", 404);

			package.SmallCollectionPointsId = model.SmallCollectionPointsId;

			var currentProductsInPackage = await _productService.GetProductsByPackageIdAsync(model.PackageId);

			var newQrCodesSet = model.ProductsQrCode.ToHashSet();

			foreach (var existingProduct in currentProductsInPackage)
			{
				if (!newQrCodesSet.Contains(existingProduct.QrCode))
				{
					await _productService.AddPackageIdToProductByQrCode(existingProduct.QrCode, null);

					await _productService.UpdateProductStatusByQrCode(existingProduct.QrCode, ProductStatus.NHAP_KHO.ToString());

					var oldHistory = await _productStatusHistoryRepository.GetAsync(h => h.ProductId == existingProduct.ProductId && h.Status == ProductStatus.DA_DONG_THUNG.ToString());
					if (oldHistory != null)
					{
						_unitOfWork.ProductStatusHistory.Delete(oldHistory);
					}

				}
			}

			foreach (var qrCode in model.ProductsQrCode)
			{
				var product = await _productService.GetByQrCode(qrCode);
				if (product != null)
				{
					await _productService.AddPackageIdToProductByQrCode(product.QrCode, package.PackageId);

					await _productService.UpdateProductStatusByQrCode(product.QrCode, ProductStatus.DA_DONG_THUNG.ToString());
				}
			}
			_unitOfWork.Packages.Update(package);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<bool> UpdatePackageStatus(string packageId, string status)
		{
			var package = await _packageRepository.GetAsync(p => p.PackageId == packageId);

			if (package == null) throw new AppException("Không tìm thấy package", 404);
			var products = await _productService.GetProductsByPackageIdAsync(packageId);
			if (products.Count == 0 ) throw new AppException("Package không có sản phẩm nào", 400);

			var statusEnum = StatusEnumHelper.GetValueFromDescription<PackageStatus>(status);
			package.Status = statusEnum.ToString();
			_unitOfWork.Packages.Update(package);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<bool> UpdatePackageStatusDeliveryAndRecycler(string packageId, string status)
		{
			var package = await _packageRepository.GetAsync(p => p.PackageId == packageId);
			var productList = await _productService.GetProductsByPackageIdAsync(packageId);
			if (package == null)
			{
				return false;
			}
			var statusEnum = StatusEnumHelper.GetValueFromDescription<PackageStatus>(status);
			package.Status = statusEnum.ToString();
			foreach (var product in productList)
			{
				await _productService.UpdateProductStatusByQrCode(product.QrCode, status);
				var newHistory = new ProductStatusHistory
				{
					ProductStatusHistoryId = Guid.NewGuid(),
					ProductId = product.ProductId,
					ChangedAt = DateTime.UtcNow,
					StatusDescription = statusEnum.ToString() == PackageStatus.DANG_VAN_CHUYEN.ToString() ? "Sản phẩm đang được vận chuyển" : "Sản phẩm đã được tái chế",
					Status = statusEnum.ToString()
				};
				await _unitOfWork.ProductStatusHistory.AddAsync(newHistory);
			}
			_unitOfWork.Packages.Update(package);
			await _unitOfWork.SaveAsync();
			return true;
		}
    }
}
