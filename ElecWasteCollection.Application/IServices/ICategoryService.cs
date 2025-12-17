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

	}
}
