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
			// 1. Lấy toàn bộ danh mục hiện có trong DB (bao gồm cả ẩn)
			var dbCategories = await _unitOfWork.Categories.GetAllAsync();

			// Chuẩn hóa tên từ Excel để so sánh nhanh
			var excelNamesLower = excelCategories
				.Select(x => x.Name.Trim().ToLower())
				.Distinct()
				.ToList();

			// 2. CẬP NHẬT TRẠNG THÁI (Dọn dẹp những cái không có trong file Excel mới)
			foreach (var dbCat in dbCategories)
			{
				var targetStatus = excelNamesLower.Contains(dbCat.Name.Trim().ToLower())
					? CategoryStatus.HOAT_DONG.ToString()
					: CategoryStatus.KHONG_HOAT_DONG.ToString();

				if (dbCat.Status != targetStatus)
				{
					dbCat.Status = targetStatus;
					_unitOfWork.Categories.Update(dbCat);
				}
			}
			await _unitOfWork.SaveAsync();

			// 3. THÊM MỚI HOẶC CẬP NHẬT THÔNG TIN CƠ BẢN
			foreach (var excelCat in excelCategories)
			{
				var existing = dbCategories.FirstOrDefault(c => c.Name.Trim().ToLower() == excelCat.Name.Trim().ToLower());

				if (existing != null)
				{
					// Cập nhật thông số tính toán
					existing.DefaultWeight = excelCat.DefaultWeight;
					existing.EmissionFactor = excelCat.EmissionFactor;
					_unitOfWork.Categories.Update(existing);
				}
				else
				{
					// Tạo mới nếu tên chưa tồn tại
					var newCat = new Category
					{
						CategoryId = Guid.NewGuid(),
						Name = excelCat.Name.Trim(),
						DefaultWeight = excelCat.DefaultWeight,
						EmissionFactor = excelCat.EmissionFactor,
						Status = CategoryStatus.HOAT_DONG.ToString()
					};
					await _unitOfWork.Categories.AddAsync(newCat);
				}
			}
			await _unitOfWork.SaveAsync();

			var updatedDbCategories = await _unitOfWork.Categories.GetAllAsync();

			foreach (var excelCat in excelCategories)
			{
				var currentCat = updatedDbCategories.FirstOrDefault(c => c.Name.Trim().ToLower() == excelCat.Name.Trim().ToLower());

				if (currentCat == null) continue;

				var parentName = excelCat.ParentName?.Trim();
				if (!string.IsNullOrEmpty(parentName))
				{
					var parentCat = updatedDbCategories.FirstOrDefault(c => c.Name.Trim().ToLower() == parentName.ToLower());

					if (parentCat != null)
					{
						if (currentCat.CategoryId != parentCat.CategoryId)
						{
							currentCat.ParentCategoryId = parentCat.CategoryId;
							_unitOfWork.Categories.Update(currentCat);
						}
					}
				}
				else
				{
					if (currentCat.ParentCategoryId != null)
					{
						currentCat.ParentCategoryId = null;
						_unitOfWork.Categories.Update(currentCat);
					}
				}
			}

			await _unitOfWork.SaveAsync();
		}
	}
}
