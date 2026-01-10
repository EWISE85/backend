using ElecWasteCollection.Application.Helper;
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

        public async Task<List<ReassignCandidateDto>> GetReassignCandidatesAsync(string smallCollectionPointId, DateTime workDateInput)
        {
            // 1. Cấu hình múi giờ Việt Nam
            TimeZoneInfo vnTimeZone;
            try
            {
                vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            }
            catch
            {
                vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            }

            var nowUtc = DateTime.UtcNow;
            var nowVn = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, vnTimeZone);

            var targetDate = DateOnly.FromDateTime(workDateInput);
            var isToday = targetDate == DateOnly.FromDateTime(nowVn);

            var allCollectors = await _unitOfWork.Users.GetAllAsync(
                u => u.Role == UserRole.Collector.ToString() &&
                     u.SmallCollectionPointId == smallCollectionPointId &&
                     u.Status == UserStatus.DANG_HOAT_DONG.ToString()
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
                    (s.Status == ShiftStatus.CO_SAN.ToString())
                ).ToList();

                if (availableShifts.Any())
                {
                    double totalRemainingMinutes = 0;
                    var displayTimeSlots = new List<string>();

                    foreach (var shift in availableShifts)
                    {
                        var startUtc = DateTime.SpecifyKind(shift.Shift_Start_Time, DateTimeKind.Utc);
                        var endUtc = DateTime.SpecifyKind(shift.Shift_End_Time, DateTimeKind.Utc);

                        var startVn = TimeZoneInfo.ConvertTimeFromUtc(startUtc, vnTimeZone);
                        var endVn = TimeZoneInfo.ConvertTimeFromUtc(endUtc, vnTimeZone);

                        if (isToday)
                        {
                            if (endVn > nowVn)
                            {
                                displayTimeSlots.Add($"{startVn:HH:mm}-{endVn:HH:mm}");
                                var startTimeToCalc = startVn > nowVn ? startVn : nowVn;
                                totalRemainingMinutes += (endVn - startTimeToCalc).TotalMinutes;
                            }
                        }
                        else
                        {
                            displayTimeSlots.Add($"{startVn:HH:mm}-{endVn:HH:mm}");
                            totalRemainingMinutes += (endVn - startVn).TotalMinutes;
                        }
                    }
                    if (totalRemainingMinutes > 0)
                    {
                        var timeSlotsStr = string.Join(", ", displayTimeSlots.OrderBy(x => x));
                        var roundedMinutes = Math.Round(totalRemainingMinutes, 2);

                        result.Add(new ReassignCandidateDto
                        {
                            UserId = user.UserId,
                            Name = user.Name,
                            Phone = user.Phone,
                            IsAvailable = true,
                            ShiftTime = timeSlotsStr,
                            StatusText = StatusEnumHelper.GetDescription(ShiftStatus.CO_SAN),
                            RemainingMinutes = $"{roundedMinutes} phút",
                            SortableMinutes = roundedMinutes
                        });
                    }
                }
            }

            return result
                .OrderByDescending(x => x.SortableMinutes)
                .ThenBy(x => GetFirstName(x.Name))
                .ToList();
        }

        private string GetFirstName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
            var parts = fullName.Trim().Split(' ');
            return parts.LastOrDefault() ?? fullName;
        }


        public async Task<ReassignDriverResponse> ReassignDriverAsync(ReassignDriverRequest request)
        {
            var group = await _unitOfWork.CollectionGroups.GetByIdAsync(request.GroupId)
                ?? throw new Exception("Không tìm thấy nhóm thu gom.");

            var oldShift = await _unitOfWork.Shifts.GetByIdAsync(group.Shift_Id)
                ?? throw new Exception("Không tìm thấy ca làm việc gốc.");

            //if (oldShift.Status == "Completed")
            //    throw new Exception("Lộ trình này đã hoàn thành, không thể thay thế.");

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
                s.Status != ShiftStatus.DA_HUY.ToString() &&
                s.Shift_Start_Time < reqEnd &&
                s.Shift_End_Time > reqStart
            );

            if (conflictShifts.Any())
            {
                var busyShift = conflictShifts.First();
                var busyVehicle = await _unitOfWork.Vehicles.GetByIdAsync(busyShift.Vehicle_Id);

                throw new Exception($"Nhân viên {newCollector.Name} đang bận chạy xe {busyVehicle?.Plate_Number ?? "khác"} ");
            }

            oldShift.Status = ShiftStatus.DA_HUY.ToString();
            oldShift.Vehicle_Id = null;
            _unitOfWork.Shifts.Update(oldShift);

            var availableShifts = await _unitOfWork.Shifts.GetAllAsync(s =>
                s.CollectorId == request.NewCollectorId &&
                s.WorkDate == workDate &&
                string.IsNullOrEmpty(s.Vehicle_Id) &&
                (s.Status == ShiftStatus.CO_SAN.ToString()) &&
                s.Shift_Start_Time < reqEnd &&
                s.Shift_End_Time > reqStart
            );

            var availableShift = availableShifts.FirstOrDefault();
            string targetShiftId;
            string messageAction;

            if (availableShift != null)
            {
                availableShift.Vehicle_Id = vehicleId;
                availableShift.Status = ShiftStatus.DA_LEN_LICH.ToString();
                _unitOfWork.Shifts.Update(availableShift);

                targetShiftId = availableShift.ShiftId;
                messageAction = "Đã cập nhật vào lịch chờ có sẵn";
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
                    Status = ShiftStatus.DA_LEN_LICH.ToString()
                };

                await _unitOfWork.Shifts.AddAsync(newShift);
                targetShiftId = newId;
                messageAction = "Đã tạo ca làm việc mới";
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