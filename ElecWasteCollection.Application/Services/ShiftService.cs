using DocumentFormat.OpenXml.Spreadsheet;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class ShiftService : IShiftService
	{
		private readonly IShiftRepository _shiftRepository;
		private readonly IUnitOfWork _unitOfWork;
		public ShiftService(IShiftRepository shiftRepository, IUnitOfWork unitOfWork)
		{
			_shiftRepository = shiftRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<ImportResult> CheckAndUpdateShiftAsync(CreateShiftModel shift)
		{
			var result = new ImportResult();
			var existingShift = await _shiftRepository.GetAsync(s => s.ShiftId == shift.ShiftId);
			if (existingShift != null)
			{
				existingShift.CollectorId = shift.CollectorId;
				existingShift.WorkDate = shift.WorkDate;
				existingShift.Shift_Start_Time = shift.Shift_Start_Time;
				existingShift.Shift_End_Time = shift.Shift_End_Time;
				existingShift.Status = shift.Status;
				_unitOfWork.Shifts.Update(existingShift);
			}
			else
			{
				var newShift = new Shifts
				{
					ShiftId = shift.ShiftId,
					CollectorId = shift.CollectorId,
					WorkDate = shift.WorkDate,
					Shift_Start_Time = shift.Shift_Start_Time,
					Shift_End_Time = shift.Shift_End_Time,
					Status = shift.Status,
				};
				await _unitOfWork.Shifts.AddAsync(newShift);
			}
			await _unitOfWork.SaveAsync();
			return result;
		}

		public async Task<bool> CreateShiftAsync(CreateShiftModel newShift)
		{
			var shift = new Shifts
			{
				ShiftId = newShift.ShiftId,
				CollectorId = newShift.CollectorId,
				Shift_Start_Time = newShift.Shift_Start_Time,
				Shift_End_Time = newShift.Shift_End_Time,
				Status = newShift.Status,
			};
			await _unitOfWork.Shifts.AddAsync(shift);
			await _unitOfWork.SaveAsync();
			return true;
		}

		//public async Task<bool> DeleteShiftAsync(string shiftId)
		//{
		//	var shift = await _shiftRepository.GetAsync(s => s.ShiftId == shiftId);
		//	if (shift == null) throw new AppException("Không tìm thấy ca làm", 404);
		//	shift.Status = ShiftStatus.KHONG_CO.ToString();
		//	_unitOfWork.Shifts.Update(shift);
		//	await _unitOfWork.SaveAsync();
		//	return true;
		//}

		public async Task<PagedResultModel<ShiftModel>> GetPagedShiftAsync(ShiftSearchModel model)
		{
			var (shifts, totalItems) = await _shiftRepository.GetPagedShiftsAsync(
				collectionCompanyId: model.CollectionCompanyId,
				smallCollectionPointId: model.SmallCollectionPointId,
				status: model.Status,
				fromDate: model.FromDate,
				toDate: model.ToDate,
				page: model.Page,
				limit: model.Limit
			);
			var resultList = shifts.Select(s => new ShiftModel
			{
				ShiftId = s.ShiftId.ToString(),
				CollectorId = s.CollectorId,
				CollectorName = s.Collector?.Name ?? "N/A",
				Vehicle_Id = s.Vehicle_Id?.ToString(),
				Plate_Number = s.Vehicle?.Plate_Number ?? "Chưa gán xe",
				WorkDate = s.WorkDate,
				Shift_Start_Time = s.Shift_Start_Time,
				Shift_End_Time = s.Shift_End_Time,
				Status = s.Status
			}).ToList();
			return new PagedResultModel<ShiftModel>(
				resultList,
				model.Page,
				model.Limit,
				totalItems
			);
		}

		public async Task<ShiftModel> GetShiftById(string shiftId)
		{
			var shift = await _shiftRepository.GetShiftWithDetailsAsync(shiftId);
			if (shift == null) throw new AppException("Không tìm thấy ca làm việc", 404);
			return new ShiftModel
			{
				ShiftId = shift.ShiftId.ToString(),
				CollectorId = shift.CollectorId,
				CollectorName = shift.Collector?.Name ?? "Không rõ",
				Vehicle_Id = shift.Vehicle_Id?.ToString(),
				Plate_Number = shift.Vehicle?.Plate_Number ?? "Chưa gán xe",
				WorkDate = shift.WorkDate,
				Shift_Start_Time = shift.Shift_Start_Time,
				Shift_End_Time = shift.Shift_End_Time,
				Status = shift.Status
			};
		}

		public async Task<bool> UpdateShiftAsync(CreateShiftModel updateShift)
		{
			var shift = await _shiftRepository.GetAsync(s => s.ShiftId == updateShift.ShiftId);
			if (shift == null) throw new AppException("Không tìm thấy ca làm việc", 404);

			shift.CollectorId = updateShift.CollectorId;
			shift.Shift_Start_Time = updateShift.Shift_Start_Time;
			shift.Shift_End_Time = updateShift.Shift_End_Time;
			shift.Status = updateShift.Status;
			_unitOfWork.Shifts.Update(shift);
			await _unitOfWork.SaveAsync();
			return true;

		}
	}
}
