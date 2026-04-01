using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface ICategoryAttributeService
	{
		Task<List<CategoryAttributeModel>> GetCategoryAttributesByCategoryIdAsync(Guid categoryId);
		Task SyncCategoryAttributeMapsAsync(List<CategoryAttributeMapModel> excelMaps);
		Task<List<CategoryAttributeModel>> GetAttributeByCategoryIdForAdmin(Guid categoryId, string? status);

	}
}
