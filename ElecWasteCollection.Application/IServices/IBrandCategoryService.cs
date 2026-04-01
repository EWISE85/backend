using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface IBrandCategoryService
    {
        Task<double> EstimatePointForBrandAndCategory(Guid categoryId, Guid brandId);
        Task SyncBrandCategoryMapsAsync(List<BrandCategoryMapModel> excelMaps);

		Task<PagedResultModel<BrandCategoryMapModel>> GetPagedBrandForAdmin(Guid categoryId, string? brandName, string? status, int page, int limit);

        Task<bool> DeleteBrandCategory(Guid categoryId, Guid brandId);
	}
}
