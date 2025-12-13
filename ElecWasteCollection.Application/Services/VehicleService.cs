using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class VehicleService : IVehicleService
	{
		private readonly List<Vehicles> vehicles = FakeDataSeeder.vehicles;
		private readonly List<SmallCollectionPoints> _smallCollectionPoints = FakeDataSeeder.smallCollectionPoints;
		public Task<ImportResult> CheckAndUpdateVehicleAsync(CreateVehicleModel vehicle)
		{
			var importResult = new ImportResult();
			var existingVehicle = vehicles
				.FirstOrDefault(v => v.VehicleId == vehicle.VehicleId);
			if (existingVehicle != null)
			{
				existingVehicle.Plate_Number = vehicle.Plate_Number;
				existingVehicle.Vehicle_Type = vehicle.Vehicle_Type;
				existingVehicle.Capacity_Kg = vehicle.Capacity_Kg;
				existingVehicle.Capacity_M3 = vehicle.Capacity_M3;
				existingVehicle.Status = vehicle.Status;
				existingVehicle.Small_Collection_Point = vehicle.Small_Collection_Point;
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
				vehicles.Add(newVehicle);

			}
				return Task.FromResult(importResult);
		}

	public VehicleModel? GetVehicleById(string vehicleId)
		{
			var vehicle = vehicles.FirstOrDefault(v => v.VehicleId == vehicleId);
			if (vehicle == null)
			{
				return null;
			}
			var smallCollectionPoint = _smallCollectionPoints
				.FirstOrDefault(scp => scp.SmallCollectionPointsId == vehicle.Small_Collection_Point);
			return new VehicleModel
			{
				VehicleId = vehicle.VehicleId,
				PlateNumber = vehicle.Plate_Number,
				VehicleType = vehicle.Vehicle_Type,
				CapacityKg = vehicle.Capacity_Kg,
				CapacityM3 = vehicle.Capacity_M3,
				Status = vehicle.Status,
				SmallCollectionPointId = vehicle.Small_Collection_Point,
				SmallCollectionPointName = smallCollectionPoint?.Name ?? "Chưa gán điểm thu gom"
			};

		}

		public Task<PagedResultModel<VehicleModel>> PagedVehicles(VehicleSearchModel model)
		{
			// 1. Khởi tạo query
			var query = vehicles.AsQueryable(); // _vehicles là List giả lập hoặc DbSet

			// 2. Xử lý bộ lọc

			// 2a. Lọc theo CollectionCompanyId (Logic bắc cầu: Company -> SCP -> Vehicle)
			if (!string.IsNullOrEmpty(model.CollectionCompanyId))
			{
				// Tìm danh sách ID của các SCP thuộc Company này
				// Lưu ý: Cần convert ID sang cùng kiểu dữ liệu (Guid hoặc String) để so sánh
				var scpIdsInCompany = _smallCollectionPoints
					.Where(scp => scp.CompanyId != null &&
								  scp.CompanyId.ToString().Equals(model.CollectionCompanyId, StringComparison.OrdinalIgnoreCase))
					.Select(scp => scp.SmallCollectionPointsId.ToString()) // Giả sử Vehicle lưu SCP ID dưới dạng string hoặc Guid
					.ToList();

				// Lọc xe thuộc các SCP này
				query = query.Where(v => v.Small_Collection_Point != null &&
										 scpIdsInCompany.Contains(v.Small_Collection_Point));
			}

			// 2b. Lọc theo SmallCollectionPointId (Trực tiếp)
			if (!string.IsNullOrEmpty(model.SmallCollectionPointId))
			{
				query = query.Where(v => v.Small_Collection_Point != null &&
										 v.Small_Collection_Point.ToString().Equals(model.SmallCollectionPointId, StringComparison.OrdinalIgnoreCase));
			}

			// 2c. Lọc theo Biển số (PlateNumber) - Tìm gần đúng (Contains)
			if (!string.IsNullOrEmpty(model.PlateNumber))
			{
				query = query.Where(v => v.Plate_Number.Contains(model.PlateNumber, StringComparison.OrdinalIgnoreCase));
			}

			// 2d. Lọc theo Trạng thái (Status) - Tìm chính xác
			if (!string.IsNullOrEmpty(model.Status))
			{
				query = query.Where(v => v.Status.Equals(model.Status, StringComparison.OrdinalIgnoreCase));
			}

			// 3. Tính tổng số bản ghi (cho phân trang)
			var totalItems = query.Count();

			// 4. Phân trang & Lấy dữ liệu (Execute query)
			var pagedEntities = query
				.Skip((model.Page - 1) * model.Limit)
				.Take(model.Limit)
				.ToList();

			// 5. Mapping sang VehicleModel
			var resultList = pagedEntities.Select(v =>
			{
				// Lookup tên Small Collection Point để hiển thị
				var scp = _smallCollectionPoints
					.FirstOrDefault(s => s.SmallCollectionPointsId == v.Small_Collection_Point);

				return new VehicleModel
				{
					VehicleId = v.VehicleId.ToString(),
					PlateNumber = v.Plate_Number,
					VehicleType = v.Vehicle_Type,
					CapacityKg = v.Capacity_Kg, 
					CapacityM3 = v.Capacity_M3, 
					Status = v.Status,
					SmallCollectionPointId = v.Small_Collection_Point,
					// Map tên SCP, nếu không có thì báo N/A
					SmallCollectionPointName = scp?.Name ?? "Chưa gán điểm thu gom"
				};
			}).ToList();

			// 6. Đóng gói kết quả
			var pagedResult = new PagedResultModel<VehicleModel>(resultList, model.Page, model.Limit, totalItems);

			return Task.FromResult(pagedResult);
		}
	}
}
