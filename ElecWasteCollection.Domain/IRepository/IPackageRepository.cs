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
	}
}
