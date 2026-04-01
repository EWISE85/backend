using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface IBrandService
    {
		Task<List<BrandModel>> GetBrandsByCategoryIdAsync(Guid categoryId);
		Task<Guid> CheckAndUpdateBrandAsync(string brandName);
		Task SyncBrandsAsync(List<string> excelBrandNames);
	}
}
