using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IVehicleService
	{
		Task<VehicleModel?> GetVehicleById(string vehicleId);

		Task<PagedResultModel<VehicleModel>> PagedVehicles(VehicleSearchModel model);

		Task<ImportResult> CheckAndUpdateVehicleAsync(CreateVehicleModel vehicle);

	}
}
