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
	public class CategoryService : ICategoryService
	{
		private readonly ICategoryRepository _categoryRepository;
		private readonly IUnitOfWork _unitOfWork;
		public CategoryService(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork)
		{
			_categoryRepository = categoryRepository;
			_unitOfWork = unitOfWork;
		}

		//public async Task<bool> ActiveChildCategory(Guid categoryId)
		//{
		//	var childCategory = await _categoryRepository.GetAsync(c => c.CategoryId == categoryId && c.Status == CategoryStatus.KHONG_HOAT_DONG.ToString());
		//	if (childCategory == null) throw new AppException("Không tìm thấy danh mục con hoặc đã được kích hoạt", 404);
		//	childCategory.Status = CategoryStatus.HOAT_DONG.ToString();
		//	_unitOfWork.Categories.Update(childCategory);
		//	await _unitOfWork.SaveAsync();
		//	return true;
		//}

		//public async Task<bool> ActiveParentCategory(Guid categoryId)
		//{
		//	var childCategory =  await _categoryRepository.GetAsync(c => c.CategoryId == categoryId && c.ParentCategoryId == null && c.Status == CategoryStatus.KHONG_HOAT_DONG.ToString());
		//	if (childCategory == null) throw new AppException("Không tìm thấy danh mục con hoặc đã được kích hoạt", 404);
		//	childCategory.Status = CategoryStatus.HOAT_DONG.ToString();
		//	_unitOfWork.Categories.Update(childCategory);
		//	await _unitOfWork.SaveAsync();
		//	return true;
		//}

		//public async Task<bool> DeleteChildCategory(Guid categoryId)
		//{
		//	var childCategory = await _categoryRepository.GetAsync(c => c.CategoryId == categoryId && c.Status == CategoryStatus.HOAT_DONG.ToString());
		//	if (childCategory == null) throw new AppException("Không tìm thấy danh mục con hoặc đã bị xóa", 404);
		//	childCategory.Status = CategoryStatus.KHONG_HOAT_DONG.ToString();
		//	_unitOfWork.Categories.Update(childCategory);
		//	await _unitOfWork.SaveAsync();
		//	return true;
		//}

		//public async Task<bool> DeleteParentCategory(Guid categoryId)
		//{
		//	var parentCategory = await _categoryRepository.GetAsync(c => c.CategoryId == categoryId && c.ParentCategoryId == null && c.Status == CategoryStatus.HOAT_DONG.ToString());
		//	if (parentCategory == null) throw new AppException("Không tìm thấy danh mục cha hoặc đã bị xóa", 404);
		//	parentCategory.Status = CategoryStatus.KHONG_HOAT_DONG.ToString();
		//	_unitOfWork.Categories.Update(parentCategory);
		//	await _unitOfWork.SaveAsync();
		//	return true;
		//}

		public async Task<List<CategoryModel>> GetParentCategory()
		{

			var parentCategories = await _categoryRepository.GetsAsync(c => c.ParentCategoryId == null && c.Status == CategoryStatus.HOAT_DONG.ToString());
			if (parentCategories == null)
			{
				return new List<CategoryModel>();
			}
			var response = parentCategories.Select(c => new CategoryModel
			{
				Id = c.CategoryId,
				Name = c.Name,
				ParentCategoryId = c.ParentCategoryId
			}).ToList();
			return response;
		}

		public async Task<List<CategoryModel>> GetParentCategoryForAdmin(string? status)
		{
			string statusEnum = null;
			if (!string.IsNullOrEmpty(status))
			{
				statusEnum = StatusEnumHelper.GetValueFromDescription<CategoryStatus>(status).ToString();
			}
			var categories = await _categoryRepository.GetsAsync(c => c.ParentCategoryId == null && c.Status == statusEnum);
			if (categories == null)
			{
				return new List<CategoryModel>();
			}
			var response = categories.Select(c => new CategoryModel
			{
				Id = c.CategoryId,
				Name = c.Name,
				ParentCategoryId = c.ParentCategoryId,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<CategoryStatus>(c.Status)
			}).ToList();
			return response;
		}

		public async Task<List<CategoryModel>> GetParentCategoryWithCollectionUnit(string collectionUnitId)
		{
			// 1. Lấy thông tin CollectionUnit để xác định CompanyId
			var unit = await _unitOfWork.CollectionUnits.GetAsync(x => x.CollectionUnitId == collectionUnitId);

			if (unit == null)
			{
				return new List<CategoryModel>();
			}

			var companyId = unit.CompanyId;

			var recyclingCategories = await _unitOfWork.CompanyRecyclingCategories.GetAllAsync(
				filter: crc => crc.CompanyId == companyId
							   && crc.Category.ParentCategoryId == null
							   && crc.Category.Status == CategoryStatus.HOAT_DONG.ToString(),
				includeProperties: "Category"
			);

			var result = recyclingCategories
				.Select(crc => crc.Category) 
				.Select(c => new CategoryModel
				{
					Id = c.CategoryId,
					Name = c.Name,
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<CategoryStatus>(c.Status)
				})
				.ToList();

			return result;
		}

		public async Task<List<CategoryModel>> GetSubCategoryByName(string name, Guid parentId)
		{			
			var categories = await _categoryRepository.GetsAsync(c => c.Name.ToLower().Contains(name.ToLower()) && c.ParentCategoryId == parentId && c.Status == CategoryStatus.HOAT_DONG.ToString());
			if (categories == null)
			{
				return new List<CategoryModel>();
			}
			var subCategories = categories
				.Select(c => new CategoryModel
				{
					Id = c.CategoryId,
					Name = c.Name,
					ParentCategoryId = c.ParentCategoryId
				})
				.ToList();
			return subCategories;
		}

		public async Task<List<CategoryModel>> GetSubCategoryByParentId(Guid parentId)
		{
			var subCategories = await _categoryRepository.GetsAsync(c => c.ParentCategoryId == parentId && c.Status == CategoryStatus.HOAT_DONG.ToString());
			if (subCategories == null)
			{
				return new List<CategoryModel>();
			}
			var response = subCategories.Select(c => new CategoryModel
			{
				Id = c.CategoryId,
				Name = c.Name,
				ParentCategoryId = c.ParentCategoryId
			}).ToList();

			return response;
		}

		public async Task<PagedResultModel<CategoryModel>> GetSubCategoryByParentIdForAdmin(Guid parentId, string? name, string? status, int page, int limit)
		{
			string statusEnum = null;
			if (!string.IsNullOrEmpty(status))
			{
				statusEnum = StatusEnumHelper.GetValueFromDescription<CategoryStatus>(status).ToString();
			}
			var (category,total) = await _categoryRepository.GetPagedCategoryForAdmin(parentId,name, statusEnum, page,limit);
			var response = category.Select(c => new CategoryModel
			{
				Id = c.CategoryId,
				Name = c.Name,
				ParentCategoryId = c.ParentCategoryId,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<CategoryStatus>(c.Status)
			}).ToList();
			return new PagedResultModel<CategoryModel>(
				response,
				page,
				limit,
				total
			);
		}

		public async Task SyncCategoriesAsync(List<CategoryImportModel> excelCategories)
		{
			var dbCategories = (await _unitOfWork.Categories.GetAllAsync()).ToList();

			var excelNamesLower = excelCategories
				.Select(x => x.Name.Trim().ToLower())
				.Distinct()
				.ToList();

			foreach (var dbCat in dbCategories)
			{
				var excelMatch = excelCategories.FirstOrDefault(x => x.Name.Trim().ToLower() == dbCat.Name.Trim().ToLower());

				if (excelMatch != null)
				{
					dbCat.Status = CategoryStatus.HOAT_DONG.ToString();
					dbCat.DefaultWeight = excelMatch.DefaultWeight;
					dbCat.EmissionFactor = excelMatch.EmissionFactor;
					dbCat.AiRecognitionTags = excelMatch.AiRecognitionTags;
					_unitOfWork.Categories.Update(dbCat);
				}
				else
				{
					dbCat.Status = CategoryStatus.KHONG_HOAT_DONG.ToString();
					_unitOfWork.Categories.Update(dbCat);
				}
			}

			foreach (var excelCat in excelCategories)
			{
				var isExist = dbCategories.Any(c => c.Name.Trim().ToLower() == excelCat.Name.Trim().ToLower());

				if (!isExist)
				{
					var newCat = new Category
					{
						CategoryId = Guid.NewGuid(),
						Name = excelCat.Name.Trim(),
						DefaultWeight = excelCat.DefaultWeight,
						EmissionFactor = excelCat.EmissionFactor,
						AiRecognitionTags = excelCat.AiRecognitionTags,
						Status = CategoryStatus.HOAT_DONG.ToString()
					};
					await _unitOfWork.Categories.AddAsync(newCat);

					dbCategories.Add(newCat);
				}
			}

			foreach (var excelCat in excelCategories)
			{
				var currentCat = dbCategories.FirstOrDefault(c => c.Name.Trim().ToLower() == excelCat.Name.Trim().ToLower());
				if (currentCat == null) continue;

				var parentName = excelCat.ParentName?.Trim();
				if (!string.IsNullOrEmpty(parentName))
				{
					var parentCat = dbCategories.FirstOrDefault(c => c.Name.Trim().ToLower() == parentName.ToLower());

					// Tránh tự tham chiếu chính mình và chỉ update khi có thay đổi
					if (parentCat != null && currentCat.CategoryId != parentCat.CategoryId)
					{
						if (currentCat.ParentCategoryId != parentCat.CategoryId)
						{
							currentCat.ParentCategoryId = parentCat.CategoryId;
							_unitOfWork.Categories.Update(currentCat);
						}
					}
				}
				else if (currentCat.ParentCategoryId != null)
				{
					currentCat.ParentCategoryId = null;
					_unitOfWork.Categories.Update(currentCat);
				}
			}

			await _unitOfWork.SaveAsync();
		}
	}
}
