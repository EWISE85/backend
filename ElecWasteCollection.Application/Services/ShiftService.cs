using DocumentFormat.OpenXml.Spreadsheet;
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
	public class ShiftService : IShiftService
	{
		private readonly List<Shifts> shifts = FakeDataSeeder.shifts;
		private readonly List<User> collectors = FakeDataSeeder.users;
		private readonly List<Vehicles> vehicles = FakeDataSeeder.vehicles;

		public Task<ImportResult> CheckAndUpdateShiftAsync(CreateShiftModel shift)
		{
			var result = new ImportResult();
			var existingShift = shifts.FirstOrDefault(s => s.ShiftId == shift.ShiftId);
			if (existingShift != null)
			{
				// Cập nhật thông tin nếu Shift đã tồn tại
				existingShift.CollectorId = shift.CollectorId;
				existingShift.WorkDate = shift.WorkDate;
				existingShift.Shift_Start_Time = shift.Shift_Start_Time;
				existingShift.Shift_End_Time = shift.Shift_End_Time;
				existingShift.Status = shift.Status;
			}
			else
			{
				// Tạo mới Shift nếu chưa tồn tại
				var newShift = new Shifts
				{
					ShiftId = shift.ShiftId,
					CollectorId = shift.CollectorId,
					WorkDate = shift.WorkDate,
					Shift_Start_Time = shift.Shift_Start_Time,
					Shift_End_Time = shift.Shift_End_Time,
					Status = shift.Status,
				};
				shifts.Add(newShift);
			}
			return Task.FromResult(result);
		}

		public Task<bool> CreateShiftAsync(CreateShiftModel newShift)
		{
			var shift = new Shifts
			{
				ShiftId = newShift.ShiftId,
				CollectorId = newShift.CollectorId,
				Shift_Start_Time = newShift.Shift_Start_Time,
				Shift_End_Time = newShift.Shift_End_Time,
				Status = newShift.Status,
			};
			shifts.Add(shift);
			return Task.FromResult(true);
		}

		public Task<bool> DeleteShiftAsync(string shiftId)
		{
			var shift = shifts.FirstOrDefault(s => s.ShiftId == shiftId);
			if (shift != null)
			{
				shift.Status = ShiftStatus.Inactive.ToString();
				return Task.FromResult(true);
			}
			return Task.FromResult(false);
		}

		public Task<PagedResultModel<ShiftModel>> GetPagedShiftAsync(ShiftSearchModel model)
		{
			// --- BƯỚC 1: LỌC DANH SÁCH COLLECTOR HỢP LỆ TRƯỚC ---
			// Vì thông tin Company/SCP nằm ở bảng User, ta phải lọc User trước

			var validCollectorsQuery = collectors.AsEnumerable(); // _users là fake data list

			// Lọc theo Company
			if (!string.IsNullOrEmpty(model.CollectionCompanyId))
			{
				validCollectorsQuery = validCollectorsQuery.Where(u =>
					u.CollectionCompanyId != null &&
					u.CollectionCompanyId.Equals(model.CollectionCompanyId, StringComparison.OrdinalIgnoreCase));
			}

			// Lọc theo Small Collection Point
			if (!string.IsNullOrEmpty(model.SmallCollectionPointId))
			{
				validCollectorsQuery = validCollectorsQuery.Where(u =>
					u.SmallCollectionPointId != null &&
					u.SmallCollectionPointId.Equals(model.SmallCollectionPointId, StringComparison.OrdinalIgnoreCase));
			}

			// Lấy ra danh sách các Guid của Collector hợp lệ
			// Nếu không truyền filter Company/SCP thì list này chứa tất cả user, vẫn chạy đúng.
			var validCollectorIds = validCollectorsQuery.Select(u => u.UserId).ToList();


			// --- BƯỚC 2: LỌC SHIFT DỰA TRÊN DANH SÁCH COLLECTOR ID TRÊN ---

			var query = shifts.AsQueryable(); // _shifts là fake data list

			// Logic: Shift phải thuộc về một trong những Collector đã lọc được ở trên
			// Chỉ áp dụng lọc nếu người dùng CÓ truyền tham số lọc Company hoặc SCP
			if (!string.IsNullOrEmpty(model.CollectionCompanyId) || !string.IsNullOrEmpty(model.SmallCollectionPointId))
			{
				query = query.Where(s => validCollectorIds.Contains(s.CollectorId));
			}

			// Lọc theo Status của Shift (nếu có)
			if (!string.IsNullOrEmpty(model.Status))
			{
				query = query.Where(s => s.Status.Equals(model.Status, StringComparison.OrdinalIgnoreCase));
			}

			if (model.FromDate.HasValue)
			{
				query = query.Where(s => s.WorkDate >= model.FromDate.Value);
			}
			if (model.ToDate.HasValue)
			{
				query = query.Where(s => s.WorkDate <= model.ToDate.Value);
			}


			// --- BƯỚC 3: PHÂN TRANG & MAPPING (Giữ nguyên như cũ) ---

			var totalItems = query.Count();

			var pagedEntities = query
				.Skip((model.Page - 1) * model.Limit)
				.Take(model.Limit)
				.ToList();

			var resultList = pagedEntities.Select(s =>
			{
				// Lookup thông tin Collector
				var collector = collectors.FirstOrDefault(u => u.UserId == s.CollectorId);

				// Lookup thông tin Vehicle
				var vehicle = vehicles.FirstOrDefault(v => v.VehicleId == s.Vehicle_Id);

				return new ShiftModel
				{
					ShiftId = s.ShiftId.ToString(),
					CollectorId = s.CollectorId,
					CollectorName = collector?.Name ?? "N/A", // Lấy tên từ User
					Vehicle_Id = s.Vehicle_Id?.ToString(),
					Plate_Number = vehicle?.Plate_Number ?? "Chưa gán xe",
					WorkDate = s.WorkDate,
					Shift_Start_Time = s.Shift_Start_Time,
					Shift_End_Time = s.Shift_End_Time,
					Status = s.Status
				};
			}).ToList();

			var pagedResult = new PagedResultModel<ShiftModel>(resultList, model.Page, model.Limit, totalItems);

			return Task.FromResult(pagedResult);
		}

		public ShiftModel? GetShiftById(string shiftId)
		{
			var shift = shifts.FirstOrDefault(s => s.ShiftId == shiftId);
			var collectorInfo = collectors.FirstOrDefault(c => c.UserId == shift?.CollectorId);
			var vehicleInfo = vehicles.FirstOrDefault(v => v.VehicleId == shift?.Vehicle_Id);
			if (shift != null)
			{
				return new ShiftModel
				{
					ShiftId = shift.ShiftId,
					Vehicle_Id = shift.Vehicle_Id,
					Plate_Number = vehicleInfo?.Plate_Number,
					CollectorName = collectorInfo?.Name,
					CollectorId = shift.CollectorId,
					WorkDate = shift.WorkDate,
					Shift_Start_Time = shift.Shift_Start_Time,
					Shift_End_Time = shift.Shift_End_Time,
					Status = shift.Status,
				};
			}
			return null;
		}

		public Task<bool> UpdateShiftAsync(CreateShiftModel updateShift)
		{
			var shift = shifts.FirstOrDefault(s => s.ShiftId == updateShift.ShiftId);
			if (shift != null)
			{
				shift.CollectorId = updateShift.CollectorId;
				shift.Shift_Start_Time = updateShift.Shift_Start_Time;
				shift.Shift_End_Time = updateShift.Shift_End_Time;
				shift.Status = updateShift.Status;
				return Task.FromResult(true);
			}
			return Task.FromResult(false);
		}
	}
}
