using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class CategoryService : ICategoryService
	{
		private static List<Category> _categories = FakeDataSeeder.categories;
		public List<CategoryModel> GetParentCategory()
		{
			var parentCategories = _categories
				.Where(c => c.ParentCategoryId == null)
				.Select(c => new CategoryModel
				{
					Id = c.CategoryId,
					Name = c.Name,
					ParentCategoryId = c.ParentCategoryId
				})
				.ToList();

			return parentCategories;
		}

		public List<CategoryModel> GetSubCategoryByName(string name, Guid parentId)
		{
			var categories = _categories.Where(c => c.Name.ToLower().Contains(name.ToLower()) && c.ParentCategoryId == parentId);
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

		public List<CategoryModel> GetSubCategoryByParentId(Guid parentId)
		{
			var subCategories = _categories
				.Where(c => c.ParentCategoryId == parentId)
				.Select(c => new CategoryModel
				{
					Id = c.CategoryId,
					Name = c.Name,
					ParentCategoryId = c.ParentCategoryId
				})
				.ToList();

			return subCategories;
		}
	}
}
