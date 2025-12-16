using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
    public class ReassignDriverService : IReassignDriverService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ReassignDriverService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ReassignCandidateDto>> GetReassignCandidatesAsync(string companyId, DateTime workDateInput)
        {
            var targetDate = DateOnly.FromDateTime(workDateInput);

            var allCollectors = await _unitOfWork.Users.GetAllAsync(
                u => u.Role == "Collector" &&
                     u.CollectionCompanyId == companyId &&
                     u.Status == "Active"
            );

            var shiftsInDay = await _unitOfWork.Shifts.GetAllAsync(
                s => s.WorkDate == targetDate
            );

            var result = new List<ReassignCandidateDto>();
            var collectorList = allCollectors.ToList();
            var shiftList = shiftsInDay.ToList();

            foreach (var user in collectorList)
            {
                var userShifts = shiftList.Where(s => s.CollectorId == user.UserId).ToList();

                var availableShifts = userShifts.Where(s =>
                    string.IsNullOrEmpty(s.Vehicle_Id) &&
                    (s.Status == "Available" || s.Status == "Standby")
                ).ToList();

                if (availableShifts.Any())
                {
                    var timeSlots = string.Join(", ", availableShifts.Select(s => $"{s.Shift_Start_Time:HH:mm}-{s.Shift_End_Time:HH:mm}"));

                    result.Add(new ReassignCandidateDto
                    {
                        UserId = user.UserId,
                        Name = user.Name,
                        IsAvailable = true,
                        StatusText = $"Sẵn sàng (Ca chờ: {timeSlots})"
                    });
                }
            }

            return result.OrderBy(x => x.Name).ToList();
        }

        public async Task<ReassignDriverResponse> ReassignDriverAsync(ReassignDriverRequest request)
        {
            var group = await _unitOfWork.CollectionGroups.GetByIdAsync(request.GroupId)
                ?? throw new Exception("Không tìm thấy nhóm thu gom.");

            var oldShift = await _unitOfWork.Shifts.GetByIdAsync(group.Shift_Id)
                ?? throw new Exception("Không tìm thấy ca làm việc gốc.");

            if (oldShift.Status == "Completed")
                throw new Exception("Lộ trình này đã hoàn thành, không thể thay thế.");

            var vehicleId = oldShift.Vehicle_Id;
            var workDate = oldShift.WorkDate;
            var reqStart = oldShift.Shift_Start_Time;
            var reqEnd = oldShift.Shift_End_Time;

            var newCollector = await _unitOfWork.Users.GetByIdAsync(request.NewCollectorId)
                ?? throw new Exception("Nhân viên mới không tồn tại.");

            var conflictShifts = await _unitOfWork.Shifts.GetAllAsync(s =>
                s.CollectorId == request.NewCollectorId &&
                s.WorkDate == workDate &&
                !string.IsNullOrEmpty(s.Vehicle_Id) && 
                s.Status != "Cancelled" &&
                s.Shift_Start_Time < reqEnd &&
                s.Shift_End_Time > reqStart
            );

            if (conflictShifts.Any())
            {
                var busyShift = conflictShifts.First();
                var busyVehicle = await _unitOfWork.Vehicles.GetByIdAsync(busyShift.Vehicle_Id);

                throw new Exception($"Nhân viên {newCollector.Name} đang bận chạy xe {busyVehicle?.Plate_Number ?? "khác"} trong khung giờ {busyShift.Shift_Start_Time:HH:mm}-{busyShift.Shift_End_Time:HH:mm}.");
            }

            oldShift.Status = "Cancelled";
            oldShift.Vehicle_Id = null;
            _unitOfWork.Shifts.Update(oldShift);

            var availableShifts = await _unitOfWork.Shifts.GetAllAsync(s =>
                s.CollectorId == request.NewCollectorId &&
                s.WorkDate == workDate &&
                string.IsNullOrEmpty(s.Vehicle_Id) &&
                (s.Status == "Available" || s.Status == "Standby") &&
                s.Shift_Start_Time < reqEnd &&
                s.Shift_End_Time > reqStart
            );

            var availableShift = availableShifts.FirstOrDefault();
            string targetShiftId;
            string messageAction;

            if (availableShift != null)
            {
                availableShift.Vehicle_Id = vehicleId;
                availableShift.Status = "Assigned";
                _unitOfWork.Shifts.Update(availableShift);

                targetShiftId = availableShift.ShiftId;
                messageAction = "đã cập nhật vào lịch chờ có sẵn";
            }
            else
            {
                var newId = Guid.NewGuid().ToString();

                var newShift = new Shifts
                {
                    ShiftId = newId,
                    CollectorId = request.NewCollectorId,
                    Vehicle_Id = vehicleId,
                    WorkDate = workDate,
                    Shift_Start_Time = reqStart,
                    Shift_End_Time = reqEnd,
                    Status = "Assigned"
                };

                await _unitOfWork.Shifts.AddAsync(newShift);
                targetShiftId = newId;
                messageAction = "đã tạo ca làm việc mới";
            }

            group.Shift_Id = targetShiftId;

            var vehicleObj = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId);
            if (vehicleObj != null)
            {
                group.Name = $"{vehicleObj.Vehicle_Type} - {vehicleObj.Plate_Number} ({newCollector.Name})";
            }
            _unitOfWork.CollectionGroups.Update(group);

            await _unitOfWork.SaveAsync();

            return new ReassignDriverResponse
            {
                Success = true,
                Message = $"Thay thế thành công. Hệ thống {messageAction}. Tài xế {newCollector.Name} đã nhận xe {vehicleObj?.Plate_Number}.",
                GroupId = group.CollectionGroupId,
                ShiftId = targetShiftId,
                CollectorName = newCollector.Name,
                VehiclePlate = vehicleObj?.Plate_Number
            };
        }
    }
}