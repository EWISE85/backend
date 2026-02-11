using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IPackageService
	{
		Task<string> CreatePackageAsync(CreatePackageModel model);
		Task<PackageDetailModel> GetPackageById(string packageId, int page, int limit);
		Task<PagedResultModel<PackageDetailModel>> GetPackagesByQuery(PackageSearchQueryModel query);
		Task<PagedResultModel<PackageDetailModel>> GetPackagesByRecylerQuery(PackageRecyclerSearchQueryModel query);
		Task<PagedResultModel<PackageDetailModel>> GetPackagesByCompanyQuery(PackageSearchCompanyQueryModel query);

		Task<bool> UpdatePackageStatus(string packageId, string status);
		Task<bool> UpdatePackageStatusDelivery(List<string> packageIds, string status);
		Task<bool> UpdatePackageStatusRecycler(string packageId, string status);

		Task<bool> UpdatePackageAsync(UpdatePackageModel model);
		Task<List<PackageDetailModel>> GetPackagesWhenDelivery();

	}
}
