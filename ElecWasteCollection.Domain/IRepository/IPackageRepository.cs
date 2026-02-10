using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface IPackageRepository : IGenericRepository<Packages>
	{
		Task<(List<Packages> Items, int TotalCount)> GetPagedPackagesWithDetailsAsync(
			string? smallCollectionPointsId,
			string? status,
			int page,
			int limit
		);
		Task<(List<Packages> Items, int TotalCount)> GetPagedPackagesWithDetailsByRecyclerAsync(
			string? recyclerId,
			string? status,
			int page,
			int limit
		);

		Task<(List<Packages> Items, int TotalCount)> GetPagedPackagesWithDetailsByCompanyAsync(
			string? companyId,
			DateTime? startDate,
			DateTime? endDate,
			string? status,
			int page,
			int limit
		);
	}
}
