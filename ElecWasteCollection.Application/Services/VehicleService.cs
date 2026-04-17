using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;

namespace ElecWasteCollection.Application.Services
{
	public class VehicleService : IVehicleService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IVehicleRepository _vehicleRepository;
		private readonly ICollectionUnitRepository _smallCollectionRepository;
		public VehicleService(IUnitOfWork unitOfWork, IVehicleRepository vehicleRepository, ICollectionUnitRepository smallCollectionRepository)
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
				var statusEnum = StatusEnumHelper.GetValueFromDescription<VehicleStatus>(vehicle.Status);
				existingVehicle.Plate_Number = vehicle.Plate_Number;
				existingVehicle.Vehicle_Type = vehicle.Vehicle_Type;
				existingVehicle.Capacity_Kg = vehicle.Capacity_Kg;
                existingVehicle.Length_M = vehicle.Length_M;
                existingVehicle.Width_M = vehicle.Width_M;
                existingVehicle.Height_M = vehicle.Height_M; 
				existingVehicle.Status = statusEnum.ToString();
				existingVehicle.CollectionUnit = vehicle.Small_Collection_Point;
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
                    Length_M = vehicle.Length_M,
                    Width_M = vehicle.Width_M,
                    Height_M = vehicle.Height_M,
                    Status = vehicle.Status,
					CollectionUnit = vehicle.Small_Collection_Point
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
			var smallCollectionPoint = await _smallCollectionRepository.GetAsync(scp => scp.CollectionUnitId == vehicle.CollectionUnit);
			return new VehicleModel
			{
				VehicleId = vehicle.VehicleId,
				PlateNumber = vehicle.Plate_Number,
				VehicleType = vehicle.Vehicle_Type,
				CapacityKg = vehicle.Capacity_Kg,
                LengthM = vehicle.Length_M,
                WidthM = vehicle.Width_M,
                HeightM = vehicle.Height_M,
                Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<VehicleStatus>(vehicle.Status),
                SmallCollectionPointId = vehicle.CollectionUnit,
				SmallCollectionPointName = smallCollectionPoint?.Name ?? "Chưa gán điểm thu gom"
			};

		}

		public async Task<PagedResultModel<VehicleModel>> PagedVehicles(VehicleSearchModel model)
		{
			var statusEnum = string.IsNullOrEmpty(model.Status)
				? null
				: StatusEnumHelper.GetValueFromDescription<VehicleStatus>(model.Status).ToString();

			var (vehicles, totalItems) = await _vehicleRepository.GetPagedVehiclesAsync(
				collectionCompanyId: model.CollectionCompanyId,
				smallCollectionPointId: model.SmallCollectionPointId,
				plateNumber: model.PlateNumber,
				status: statusEnum,
				page: model.Page,
				limit: model.Limit
			);


			var scpIds = vehicles
				.Where(v => v.CollectionUnit != null)
				.Select(v => v.CollectionUnit)
				.Distinct()
				.ToList();

			var scpDict = new Dictionary<string, string>();
			if (scpIds.Any())
			{
				// Giả sử bạn có _scpRepository
				var scps = await _smallCollectionRepository.GetsAsync(s => scpIds.Contains(s.CollectionUnitId));
				scpDict = scps.ToDictionary(k => k.CollectionUnitId, v => v.Name);
			}

			var resultList = vehicles.Select(v =>
			{
				string scpName = "Chưa gán điểm thu gom";
				if (v.CollectionUnit != null && scpDict.ContainsKey(v.CollectionUnit))
				{
					scpName = scpDict[v.CollectionUnit];
				}

				return new VehicleModel
				{
					VehicleId = v.VehicleId.ToString(),
					PlateNumber = v.Plate_Number,
					VehicleType = v.Vehicle_Type,
					CapacityKg = v.Capacity_Kg,
                    LengthM = v.Length_M,
                    WidthM = v.Width_M,
                    HeightM = v.Height_M,
                    Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<VehicleStatus>(v.Status),
                    SmallCollectionPointId = v.CollectionUnit,
					SmallCollectionPointName = scpName
				};
			}).ToList();

			return new PagedResultModel<VehicleModel>(resultList, model.Page, model.Limit, totalItems);
		}
	}
}
