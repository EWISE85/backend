using ElecWasteCollection.Application.Data;
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
		public CategoryService(ICategoryRepository categoryRepository)
		{
			_categoryRepository = categoryRepository;
		}
		public async Task<List<CategoryModel>> GetParentCategory()
		{

			var parentCategories = await _categoryRepository.GetsAsync(c => c.ParentCategoryId == null);
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

		public async Task<List<CategoryModel>> GetSubCategoryByName(string name, Guid parentId)
		{			
			var categories = await _categoryRepository.GetsAsync(c => c.Name.ToLower().Contains(name.ToLower()) && c.ParentCategoryId == parentId);
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
			var subCategories = await _categoryRepository.GetsAsync(c => c.ParentCategoryId == parentId);
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
	}
}
