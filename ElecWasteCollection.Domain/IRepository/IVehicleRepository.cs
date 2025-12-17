using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface IVehicleRepository : IGenericRepository<Vehicles>
	{
		Task<(List<Vehicles> Items, int TotalCount)> GetPagedVehiclesAsync(
			string? collectionCompanyId,
			string? smallCollectionPointId,
			string? plateNumber,
			string? status,
			int page,
			int limit
		);
	}
}
