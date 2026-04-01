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
using static Google.Apis.Requests.BatchRequest;

namespace ElecWasteCollection.Application.Services
{
	public class BrandCategoryService : IBrandCategoryService
	{
		private readonly IBrandCategoryRepository _brandCategoryRepository;
		private readonly IBrandService _brandService;
		private readonly ICategoryRepository _categoryRepository;
		private readonly IUnitOfWork _unitOfWork;
		public BrandCategoryService(IBrandCategoryRepository brandCategoryRepository, IBrandService brandService, ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
		{
			_brandCategoryRepository = brandCategoryRepository;
			_brandService = brandService;
			_categoryRepository = categoryRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> DeleteBrandCategory(Guid categoryId, Guid brandId)
		{
			var brandCategory = await _brandCategoryRepository.GetAsync(bc => bc.CategoryId == categoryId && bc.BrandId == brandId);
			if (brandCategory == null) throw new AppException("Không tìm thấy danh mục hoặc hãng", 404);
			brandCategory.Status = BrandStatus.KHONG_HOAT_DONG.ToString();
			_unitOfWork.BrandCategories.Update(brandCategory);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<double> EstimatePointForBrandAndCategory(Guid categoryId, Guid brandId)
		{
			var brandCategory = await _brandCategoryRepository.GetAsync(bc => bc.CategoryId == categoryId && bc.BrandId == brandId);
			if (brandCategory == null) throw new AppException("Không tìm thấy loại hoặc hãng",404);
			return brandCategory.Points;
		}

		public async Task<PagedResultModel<BrandCategoryMapModel>> GetPagedBrandForAdmin(Guid categoryId, string? brandName, string? status ,int page, int limit)
		{
			string statusEnum = null;
			if (!string.IsNullOrEmpty(status))
			{
				statusEnum = StatusEnumHelper.GetValueFromDescription<BrandCategoryStatus>(status).ToString();
			}
			var (items, totalCount) = await _brandCategoryRepository.GetPagedBrandForAdmin(categoryId, brandName, statusEnum, page, limit);
			var resultItems = items.Select(bc => new BrandCategoryMapModel
			{
				BrandName = bc.Brand?.Name ?? "Không tìm thấy tên",
				CategoryName = bc.Category?.Name ?? "Không tìm thấy tên",
				Points = bc.Points,
				BrandId = bc.BrandId,
				CategoryId = bc.CategoryId,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<BrandCategoryStatus>(bc.Status)
			}).ToList();
			return new PagedResultModel<BrandCategoryMapModel>(
				resultItems,
				page,
				limit,
				totalCount
			);
		}

		public async Task SyncBrandCategoryMapsAsync(List<BrandCategoryMapModel> excelMaps)
		{
			var dbMaps = await _brandCategoryRepository.GetAllAsync();
			var categories = await _categoryRepository.GetAllAsync();
			var dbBrands = await _unitOfWork.Brands.GetAllAsync();

			// 1. SOFT DELETE: Chuyển sang KHONG_HOAT_DONG nếu bị xóa khỏi Excel
			foreach (var dbMap in dbMaps)
			{
				var cat = categories.FirstOrDefault(c => c.CategoryId == dbMap.CategoryId);
				var brand = dbBrands.FirstOrDefault(b => b.BrandId == dbMap.BrandId);

				if (cat != null && brand != null)
				{
					bool stillExists = excelMaps.Any(x =>
						x.CategoryName.Trim().Equals(cat.Name.Trim(), StringComparison.OrdinalIgnoreCase) &&
						x.BrandName.Trim().Equals(brand.Name.Trim(), StringComparison.OrdinalIgnoreCase));

					if (!stillExists && dbMap.Status != BrandCategoryStatus.KHONG_HOAT_DONG.ToString())
					{
						dbMap.Status = BrandCategoryStatus.KHONG_HOAT_DONG.ToString();
						_unitOfWork.BrandCategories.Update(dbMap);
					}
				}
			}

			// 2. UPSERT: Thêm mới hoặc Cập nhật (Kèm chốt chặn Status)
			foreach (var map in excelMaps)
			{
				var category = categories.FirstOrDefault(c => c.Name.Trim().Equals(map.CategoryName.Trim(), StringComparison.OrdinalIgnoreCase));
				var brandId = await _brandService.CheckAndUpdateBrandAsync(map.BrandName);

				if (category != null)
				{
					var existingMap = dbMaps.FirstOrDefault(bc => bc.BrandId == brandId && bc.CategoryId == category.CategoryId);

					// CHỐT CHẶN: Cha (Category) sống thì Map mới được sống
					var targetStatus = (category.Status == CategoryStatus.HOAT_DONG.ToString()) ? CategoryStatus.HOAT_DONG.ToString() : CategoryStatus.KHONG_HOAT_DONG.ToString();

					if (existingMap != null)
					{
						if (existingMap.Points != map.Points || existingMap.Status != targetStatus)
						{
							existingMap.Points = map.Points;
							existingMap.Status = targetStatus;
							_unitOfWork.BrandCategories.Update(existingMap);
						}
					}
					else
					{
						var newMap = new BrandCategory
						{
							BrandCategoryId = Guid.NewGuid(),
							BrandId = brandId,
							CategoryId = category.CategoryId,
							Points = map.Points,
							Status = targetStatus // Gán theo trạng thái của cha
						};
						await _unitOfWork.BrandCategories.AddAsync(newMap);
					}
				}
			}

			await _unitOfWork.SaveAsync();
		}
	}
}
