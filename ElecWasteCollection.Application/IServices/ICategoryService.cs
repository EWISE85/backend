using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface ICategoryService
    {
        Task<List<CategoryModel>> GetParentCategory();

		Task<List<CategoryModel>> GetSubCategoryByParentId(Guid parentId);

		Task<List<CategoryModel>> GetSubCategoryByName(string name, Guid parentId);
		Task SyncCategoriesAsync(List<CategoryImportModel> excelCategories);

		Task<List<CategoryModel>> GetParentCategoryForAdmin(string? status);

		Task<PagedResultModel<CategoryModel>> GetSubCategoryByParentIdForAdmin(Guid parentId, string? name ,string? status, int page, int limit);

		//Task<bool> DeleteChildCategory(Guid categoryId);
		//Task<bool> DeleteParentCategory(Guid categoryId);

		//Task<bool> ActiveParentCategory(Guid categoryId);
		//Task<bool> ActiveChildCategory(Guid categoryId);
		Task<List<CategoryModel>> GetParentCategoryWithCollectionUnit( string collectionUnitId);

	}
}
