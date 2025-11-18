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
		string CreatePackageAsync(CreatePackageModel model);

		PackageDetailModel GetPackageById(string packageId);

		PagedResult<PackageDetailModel> GetPackagesByQuery(PackageSearchQueryModel query);

		bool UpdatePackageStatus(string packageId, string status);

		bool UpdatePackageAsync(UpdatePackageModel model);
	}
}
