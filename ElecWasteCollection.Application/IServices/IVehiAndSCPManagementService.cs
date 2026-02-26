using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface IVehiAndSCPManagementService
    {
        Task<bool> ApproveVehicleAsync(string vehicleId);
        Task<bool> BlockVehicleAsync(string vehicleId);
        Task<bool> ApproveSmallCollectionPointAsync(string pointId);
        Task<bool> BlockSmallCollectionPointAsync(string pointId);
    }
}
