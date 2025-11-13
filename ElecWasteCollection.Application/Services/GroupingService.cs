using System.Text.Json;
using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Application.Interfaces;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;

namespace ElecWasteCollection.Application.Services
{
    public class GroupingService : IGroupingService
    {
        private sealed class TimeSlotDetailDto
        {
            public string? StartTime { get; set; }
            public string? EndTime { get; set; }
        }

        private sealed class DailyTimeSlotsDto
        {
            public string? DayName { get; set; }
            public string? PickUpDate { get; set; }
            public TimeSlotDetailDto? Slots { get; set; }
        }

        private static bool TryGetWindow(
            string rawSchedule,
            TimeOnly shiftStart,
            TimeOnly shiftEnd,
            out string pickUpDate,
            out TimeOnly windowStart,
            out TimeOnly windowEnd)
        {
            pickUpDate = "";
            windowStart = shiftStart;
            windowEnd = shiftEnd;

            if (string.IsNullOrWhiteSpace(rawSchedule))
                return false;

            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var parsed = JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>(rawSchedule, opts);
                var first = parsed?.FirstOrDefault();

                if (first?.Slots != null &&
                    TimeOnly.TryParse(first.Slots.StartTime, out var s) &&
                    TimeOnly.TryParse(first.Slots.EndTime, out var e))
                {
                    pickUpDate = first.PickUpDate ?? "";
                    windowStart = s < shiftStart ? shiftStart : s;
                    windowEnd = e > shiftEnd ? shiftEnd : e;
                    return true;
                }
            }
            catch { }

            return false;
        }

        public async Task<GroupingByPointResponse> GroupByCollectionPointAsync(GroupingByPointRequest request)
        {
            var point = FakeDataSeeder.smallCollectionPoints
                .FirstOrDefault(p => p.Id == request.CollectionPointId)
                ?? throw new Exception("Không tìm thấy trạm thu gom.");

            var nextDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            var activeShifts = FakeDataSeeder.shifts
                .Join(FakeDataSeeder.vehicles, s => s.Vehicle_Id, v => v.Id, (s, v) => new { s, v })
                .Join(FakeDataSeeder.collectors, sv => sv.s.CollectorId, c => c.CollectorId, (sv, c) => new { sv.s, sv.v, c })
                .Where(x => x.v.Small_Collection_Point == point.Id && x.s.WorkDate == nextDate)
                .ToList();

            if (!activeShifts.Any())
                throw new Exception($"Không có ca làm việc nào vào ngày {nextDate} tại trạm này.");

            var approvedPosts = FakeDataSeeder.posts
                .Where(p => p.Status.ToLower().Contains("duyệt"))
                .ToList();

            if (!approvedPosts.Any())
                throw new Exception("Không có bài đăng đã duyệt nào để gom nhóm.");

            var response = new GroupingByPointResponse
            {
                CollectionPoint = point.Name,
                ActiveShifts = activeShifts.Count,
                SavedToDatabase = request.SaveResult
            };

            int groupCounter = 1;

            foreach (var shift in activeShifts)
            {
                var vehicle = shift.v;
                var collector = shift.c;
                var radius = request.RadiusKm != 0 ? request.RadiusKm : vehicle.Radius_Km;

                var postsInRange = approvedPosts
                    .Select(p =>
                    {
                        var user = FakeDataSeeder.users.FirstOrDefault(u => u.UserId == p.SenderId);
                        if (user == null) return null;

                        var lat = user.Iat ?? point.Latitude;
                        var lng = user.Ing ?? point.Longitude;
                        var distance = GeoHelper.DistanceKm(point.Latitude, point.Longitude, lat, lng);

                        var product = FakeDataSeeder.products.FirstOrDefault(pr => pr.Id == p.ProductId);
                        var sizeTier = FakeDataSeeder.sizeTiers.FirstOrDefault(t => t.SizeTierId == product?.SizeTierId);
                        double weight = sizeTier?.EstimatedWeight ?? 10;
                        double volume = sizeTier?.EstimatedVolume ?? 1;

                        string pickDate;
                        TimeOnly slotStart;
                        TimeOnly slotEnd;

                        TryGetWindow(p.ScheduleJson ?? "", TimeOnly.MinValue, TimeOnly.MaxValue, out pickDate, out slotStart, out slotEnd);

                        return new
                        {
                            Post = p,
                            User = user,
                            Lat = lat,
                            Lng = lng,
                            Distance = distance,
                            Weight = weight,
                            Volume = volume,
                            SizeTier = sizeTier?.Name ?? "Unknown",
                            Start = slotStart,
                            End = slotEnd,
                            Schedule = p.ScheduleJson
                        };
                    })
                    .Where(x => x != null && x.Distance <= radius)
                    .OrderBy(x => x.Start)
                    .ThenBy(x => x.Distance)
                    .ToList();

                if (!postsInRange.Any()) continue;

                double curLat = point.Latitude, curLng = point.Longitude;
                double speed = vehicle.Vehicle_Type.Contains("lớn") ? 30 : 25;
                int serviceMinutes = 10;

                var currentTime = TimeOnly.FromDateTime(shift.s.Shift_Start_Time);
                var shiftEnd = TimeOnly.FromDateTime(shift.s.Shift_End_Time);

                var selectedRoutes = new List<RouteDetail>();
                var routes = new List<CollectionRoutes>();

                double totalWeight = 0;
                double totalVolume = 0;
                int order = 1;

                foreach (var x in postsInRange)
                {
                    double travelMinutes = GeoHelper.DistanceKm(curLat, curLng, x.Lat, x.Lng) / speed * 60;
                    var eta = currentTime.AddMinutes(travelMinutes);

                    if (eta < x.Start) eta = x.Start;

                    if (eta > x.End || eta > shiftEnd)
                        continue;

                    // Cập nhật tổng khối lượng/volume
                    totalWeight += x.Weight;
                    totalVolume += x.Volume;

                    routes.Add(new CollectionRoutes
                    {
                        CollectionRouteId = Guid.NewGuid(),
                        PostId = x.Post.Id,
                        CollectionDate = nextDate,
                        EstimatedTime = eta,
                        Actual_Time = new TimeOnly(0, 0),
                        Status = "Đang lên lịch"
                    });

                    selectedRoutes.Add(new RouteDetail
                    {
                        PickupOrder = order++,
                        PostId = x.Post.Id.GetHashCode(),
                        UserName = x.User.Name,
                        DistanceKm = Math.Round(x.Distance, 2),
                        Address = x.Post.Address,
                        Schedule = x.Schedule,
                        EstimatedArrival = eta.ToString("HH:mm"),
                        WeightKg = x.Weight,
                        VolumeM3 = x.Volume,
                        SizeTier = x.SizeTier
                    });

                    currentTime = eta.AddMinutes(serviceMinutes);
                    curLat = x.Lat;
                    curLng = x.Lng;

                    x.Post.Status = "Gom nhóm thành công";
                }

                if (!routes.Any()) continue;

                var group = new CollectionGroups
                {
                    Id = FakeDataSeeder.collectionGroups.Count + 1,
                    Group_Code = $"GRP-{DateTime.Now:MMddHHmm}-{groupCounter++}",
                    Name = $"{vehicle.Vehicle_Type} - {vehicle.Plate_Number}",
                    Shift_Id = shift.s.Id,
                    Created_At = DateTime.Now
                };

                if (request.SaveResult)
                {
                    FakeDataSeeder.collectionGroups.Add(group);
                    FakeDataSeeder.collectionRoutes.AddRange(routes);
                }

                response.CreatedGroups.Add(new GroupSummary
                {
                    GroupCode = group.Group_Code,
                    ShiftId = shift.s.Id,
                    Vehicle = $"{vehicle.Plate_Number} - {vehicle.Vehicle_Type} ({vehicle.Capacity_Kg}kg / {vehicle.Capacity_M3}m³, radius {radius}km)",
                    Collector = collector.Name,
                    TotalPosts = routes.Count,
                    TotalWeightKg = totalWeight,
                    TotalVolumeM3 = totalVolume,
                    Routes = selectedRoutes
                });
            }

            return await Task.FromResult(response);
        }
    }
}
