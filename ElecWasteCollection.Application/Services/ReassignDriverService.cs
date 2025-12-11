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
    public class ReassignDriverService : IReassignDriverService
    {
        public async Task<List<ReassignCandidateDto>> GetReassignCandidatesAsync(string companyId, DateTime workDateInput)
        {
            var targetDate = DateOnly.FromDateTime(workDateInput);

            var allCollectors = FakeDataSeeder.users
                .Where(u => u.Role == "Collector" && u.CollectionCompanyId == companyId)
                .ToList();

            var shiftsInDay = FakeDataSeeder.shifts
                .Where(s => s.WorkDate == targetDate)
                .ToList();

            var result = new List<ReassignCandidateDto>();

            foreach (var user in allCollectors)
            {
                var userShifts = shiftsInDay.Where(s => s.CollectorId == user.UserId).ToList();

                var busyShifts = userShifts.Where(s =>
                    !string.IsNullOrEmpty(s.Vehicle_Id) &&
                    s.Status != "Cancelled"
                ).ToList();

                var availableShifts = userShifts.Where(s =>
                    string.IsNullOrEmpty(s.Vehicle_Id) &&
                    (s.Status == "Available" || s.Status == "Standby")
                ).ToList();

                if (busyShifts.Any())
                {
                    var timeSlots = string.Join(", ", busyShifts.Select(s =>
                        $"{s.Shift_Start_Time:HH:mm}-{s.Shift_End_Time:HH:mm} (Xe: {FakeDataSeeder.vehicles.FirstOrDefault(v => v.VehicleId == s.Vehicle_Id)?.Plate_Number ?? s.Vehicle_Id})"));

                    result.Add(new ReassignCandidateDto
                    {
                        UserId = user.UserId,
                        Name = user.Name,
                        IsAvailable = false, 
                        StatusText = $"Bận: {timeSlots}"
                    });
                }
                else if (availableShifts.Any())
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
                else
                {

                    result.Add(new ReassignCandidateDto
                    {
                        UserId = user.UserId,
                        Name = user.Name,
                        IsAvailable = true, 
                        StatusText = "Sẵn sàng (Chưa có lịch, sẽ tạo mới)"
                    });
                }
            }

            return result.OrderByDescending(x => x.IsAvailable).ThenBy(x => x.Name).ToList();
        }

        public async Task<ReassignDriverResponse> ReassignDriverAsync(ReassignDriverRequest request)
        {
            var group = FakeDataSeeder.collectionGroups.FirstOrDefault(g => g.CollectionGroupId == request.GroupId)
                ?? throw new Exception("Không tìm thấy nhóm thu gom.");

            var oldShift = FakeDataSeeder.shifts.FirstOrDefault(s => s.ShiftId == group.Shift_Id)
                ?? throw new Exception("Không tìm thấy ca làm việc gốc.");

            if (oldShift.Status == "Completed")
                throw new Exception("Lộ trình này đã hoàn thành, không thể thay thế.");

            var vehicleId = oldShift.Vehicle_Id;
            var workDate = oldShift.WorkDate;
            var reqStart = oldShift.Shift_Start_Time;
            var reqEnd = oldShift.Shift_End_Time;

            var newCollector = FakeDataSeeder.users.FirstOrDefault(u => u.UserId == request.NewCollectorId)
                ?? throw new Exception("Nhân viên mới không tồn tại.");

            var isBusy = FakeDataSeeder.shifts.Any(s =>
                s.CollectorId == request.NewCollectorId &&
                s.WorkDate == workDate &&
                !string.IsNullOrEmpty(s.Vehicle_Id) && 
                s.Status != "Cancelled" &&             
                s.Shift_Start_Time < reqEnd &&        
                s.Shift_End_Time > reqStart
            );

            if (isBusy)
            {
                var busyShift = FakeDataSeeder.shifts.First(s =>
                    s.CollectorId == request.NewCollectorId && !string.IsNullOrEmpty(s.Vehicle_Id) &&
                    s.Shift_Start_Time < reqEnd && s.Shift_End_Time > reqStart);

                var busyVehicle = FakeDataSeeder.vehicles.FirstOrDefault(v => v.VehicleId == busyShift.Vehicle_Id);

                throw new Exception($"Nhân viên {newCollector.Name} đang bận chạy xe {busyVehicle?.Plate_Number ?? "khác"} trong khung giờ {busyShift.Shift_Start_Time:HH:mm}-{busyShift.Shift_End_Time:HH:mm}.");
            }

            oldShift.Status = "Cancelled";
            oldShift.Vehicle_Id = null;

            var availableShift = FakeDataSeeder.shifts.FirstOrDefault(s =>
                s.CollectorId == request.NewCollectorId &&
                s.WorkDate == workDate &&
                string.IsNullOrEmpty(s.Vehicle_Id) &&
                (s.Status == "Available" || s.Status == "Standby") &&
                s.Shift_Start_Time < reqEnd && 
                s.Shift_End_Time > reqStart
            );

            string targetShiftId;
            string messageAction;

            if (availableShift != null)
            {
                availableShift.Vehicle_Id = vehicleId;
                availableShift.Status = "Assigned";
                targetShiftId = availableShift.ShiftId;
                messageAction = "đã cập nhật vào lịch chờ có sẵn";
            }
            else
            {
                int newId = FakeDataSeeder.shifts.Any() ? FakeDataSeeder.shifts.Max(s => int.Parse(s.ShiftId)) + 1 : 1;
                var newShift = new Shifts
                {
                    ShiftId = newId.ToString(),
                    CollectorId = request.NewCollectorId,
                    Vehicle_Id = vehicleId,
                    WorkDate = workDate,
                    Shift_Start_Time = reqStart,
                    Shift_End_Time = reqEnd,
                    Status = "Assigned"
                };

                FakeDataSeeder.shifts.Add(newShift);
                targetShiftId = newId.ToString();
                messageAction = "đã tạo ca làm việc mới";
            }

            group.Shift_Id = targetShiftId;

            var vehicleObj = FakeDataSeeder.vehicles.FirstOrDefault(v => v.VehicleId == vehicleId);
            if (vehicleObj != null)
            {
                group.Name = $"{vehicleObj.Vehicle_Type} - {vehicleObj.Plate_Number} ({newCollector.Name})";
            }

            return await Task.FromResult(new ReassignDriverResponse
            {
                Success = true,
                Message = $"Thay thế thành công. Hệ thống {messageAction}. Tài xế {newCollector.Name} đã nhận xe {vehicleObj?.Plate_Number}.",
                GroupId = group.CollectionGroupId, 
                ShiftId = targetShiftId,
                CollectorName = newCollector.Name,
                VehiclePlate = vehicleObj?.Plate_Number
            });
        }
    }
}
