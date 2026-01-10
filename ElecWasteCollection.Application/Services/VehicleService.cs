using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class VehicleService : IVehicleService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IVehicleRepository _vehicleRepository;
		private readonly ISmallCollectionRepository _smallCollectionRepository;
		public VehicleService(IUnitOfWork unitOfWork, IVehicleRepository vehicleRepository, ISmallCollectionRepository smallCollectionRepository)
		{
			_unitOfWork = unitOfWork;
			_vehicleRepository = vehicleRepository;
			_smallCollectionRepository = smallCollectionRepository;
		}
		public async Task<ImportResult> CheckAndUpdateVehicleAsync(CreateVehicleModel vehicle)
		{
			var importResult = new ImportResult();
			var existingVehicle = await _vehicleRepository.GetAsync(v => v.VehicleId == vehicle.VehicleId);
			if (existingVehicle != null)
			{
				existingVehicle.Plate_Number = vehicle.Plate_Number;
				existingVehicle.Vehicle_Type = vehicle.Vehicle_Type;
				existingVehicle.Capacity_Kg = vehicle.Capacity_Kg;
				existingVehicle.Capacity_M3 = vehicle.Capacity_M3;
				existingVehicle.Status = vehicle.Status;
				existingVehicle.Small_Collection_Point = vehicle.Small_Collection_Point;
				 _unitOfWork.Vehicles.Update(existingVehicle);
			}
			else
			{
				var newVehicle = new Vehicles
				{
					VehicleId = vehicle.VehicleId,
					Plate_Number = vehicle.Plate_Number,
					Vehicle_Type = vehicle.Vehicle_Type,
					Capacity_Kg = vehicle.Capacity_Kg,
					Capacity_M3 = vehicle.Capacity_M3,
					Status = vehicle.Status,
					Small_Collection_Point = vehicle.Small_Collection_Point
				};
				await _unitOfWork.Vehicles.AddAsync(newVehicle);

			}
			await _unitOfWork.SaveAsync();
			return importResult;
		}

	public async Task<VehicleModel?> GetVehicleById(string vehicleId)
		{
			var vehicle = await _vehicleRepository.GetAsync(v => v.VehicleId == vehicleId);
			if (vehicle == null)
			{
				throw new AppException("Xe không tồn tại", 404);
			}
			var smallCollectionPoint = await _smallCollectionRepository.GetAsync(scp => scp.SmallCollectionPointsId == vehicle.Small_Collection_Point);
			return new VehicleModel
			{
				VehicleId = vehicle.VehicleId,
				PlateNumber = vehicle.Plate_Number,
				VehicleType = vehicle.Vehicle_Type,
				CapacityKg = vehicle.Capacity_Kg,
				CapacityM3 = vehicle.Capacity_M3,
                Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<VehicleStatus>(vehicle.Status),
                SmallCollectionPointId = vehicle.Small_Collection_Point,
				SmallCollectionPointName = smallCollectionPoint?.Name ?? "Chưa gán điểm thu gom"
			};

		}

		public async Task<PagedResultModel<VehicleModel>> PagedVehicles(VehicleSearchModel model)
		{

			var (vehicles, totalItems) = await _vehicleRepository.GetPagedVehiclesAsync(
				collectionCompanyId: model.CollectionCompanyId,
				smallCollectionPointId: model.SmallCollectionPointId,
				plateNumber: model.PlateNumber,
				status: model.Status,
				page: model.Page,
				limit: model.Limit
			);


			var scpIds = vehicles
				.Where(v => v.Small_Collection_Point != null)
				.Select(v => v.Small_Collection_Point)
				.Distinct()
				.ToList();

			var scpDict = new Dictionary<string, string>();
			if (scpIds.Any())
			{
				// Giả sử bạn có _scpRepository
				var scps = await _smallCollectionRepository.GetsAsync(s => scpIds.Contains(s.SmallCollectionPointsId));
				scpDict = scps.ToDictionary(k => k.SmallCollectionPointsId, v => v.Name);
			}

			var resultList = vehicles.Select(v =>
			{
				string scpName = "Chưa gán điểm thu gom";
				if (v.Small_Collection_Point != null && scpDict.ContainsKey(v.Small_Collection_Point))
				{
					scpName = scpDict[v.Small_Collection_Point];
				}

				return new VehicleModel
				{
					VehicleId = v.VehicleId.ToString(),
					PlateNumber = v.Plate_Number,
					VehicleType = v.Vehicle_Type,
					CapacityKg = v.Capacity_Kg,
					CapacityM3 = v.Capacity_M3,
                    Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<VehicleStatus>(v.Status),
                    SmallCollectionPointId = v.Small_Collection_Point,
					SmallCollectionPointName = scpName
				};
			}).ToList();

			return new PagedResultModel<VehicleModel>(resultList, model.Page, model.Limit, totalItems);
		}
	}
}
