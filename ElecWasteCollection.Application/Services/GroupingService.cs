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
        public async Task<GroupingByPointResponse> GroupByCollectionPointAsync(GroupingByPointRequest request)
        {
            var point = FakeDataSeeder.smallCollectionPoints
                .FirstOrDefault(p => p.Id == request.CollectionPointId);
            if (point == null)
                throw new Exception("Không tìm thấy trạm thu gom.");

            var nextDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

            var activeShifts = FakeDataSeeder.shifts
                .Join(FakeDataSeeder.vehicles,
                    s => s.Vehicle_Id,
                    v => v.Id,
                    (s, v) => new { s, v })
                .Where(x => x.v.Small_Collection_Point == point.Id
                            && x.s.WorkDate == nextDate)
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
                var radius = request.RadiusKm != 0 ? request.RadiusKm : vehicle.Radius_Km;

                var postsWithSchedule = approvedPosts
                    .Select(p =>
                    {
                        var user = FakeDataSeeder.users.FirstOrDefault(u => u.UserId == p.SenderId);
                        var distance = GeoHelper.DistanceKm(point.Latitude, point.Longitude, user!.Iat, user!.Ing);

                        List<TimeSlotDetail>? slots = null;
                        try
                        {
                            var parsed = JsonSerializer.Deserialize<List<DailyTimeSlots>>(p.ScheduleJson ?? "[]");
                            slots = parsed?.FirstOrDefault()?.Slots;
                        }
                        catch
                        {
                            slots = new List<TimeSlotDetail>();
                        }

                        return new { Post = p, User = user, Distance = distance, Slots = slots };
                    })
                    .Where(x => x.Distance <= radius && x.Slots != null && x.Slots.Any())
                    .ToList();

                if (!postsWithSchedule.Any())
                    continue;

                var clusters = new List<List<dynamic>>();

                foreach (var item in postsWithSchedule)
                {
                    var slot = item.Slots!.First();
                    var start = TimeOnly.Parse(slot.StartTime);
                    var end = TimeOnly.Parse(slot.EndTime);

                    var found = clusters.FirstOrDefault(c =>
                        c.Any(x =>
                        {
                            var s = TimeOnly.Parse(x.Slot.StartTime);
                            var e = TimeOnly.Parse(x.Slot.EndTime);
                            return (start <= e && end >= s) ||
                                   Math.Abs(start.ToTimeSpan().TotalMinutes - s.ToTimeSpan().TotalMinutes) <= 90;
                        })
                    );

                    if (found != null)
                        found.Add(new { item.Post, item.User, Slot = slot, item.Distance });
                    else
                        clusters.Add(new List<dynamic> { new { item.Post, item.User, Slot = slot, item.Distance } });
                }

                foreach (var cluster in clusters)
                {
                    var minStart = cluster.Min(x => TimeOnly.Parse(x.Slot.StartTime));
                    var maxEnd = cluster.Max(x => TimeOnly.Parse(x.Slot.EndTime));

                    var ordered = cluster
                        .OrderBy(x => TimeOnly.Parse(x.Slot.StartTime))
                        .ThenBy(x => x.Distance)
                        .ToList();

                    var group = new CollectionGroups
                    {
                        Id = FakeDataSeeder.collectionGroups.Count + 1,
                        Group_Code = $"GRP-{DateTime.Now:MMddHHmm}-{groupCounter++}",
                        Name = $"{vehicle.Vehicle_Type} - {vehicle.Plate_Number} ({minStart:HH\\:mm}-{maxEnd:HH\\:mm})",
                        Shift_Id = shift.s.Id,
                        Created_At = DateTime.Now
                    };

                    var routes = new List<CollectionRoutes>();
                    var routeDetails = new List<RouteDetail>();

                    int order = 1;
                    var currentTime = minStart;

                    foreach (var x in ordered)
                    {
                        var slotStart = TimeOnly.Parse(x.Slot.StartTime);
                        var slotEnd = TimeOnly.Parse(x.Slot.EndTime);

                        if (currentTime < slotStart)
                            currentTime = slotStart;

                        if (currentTime > slotEnd)
                            currentTime = slotEnd;

                        var route = new CollectionRoutes
                        {
                            CollectionRouteId = Guid.NewGuid(),
                            PostId = x.Post.Id,
                            CollectorId = FakeDataSeeder.collector.CollectorId,
                            CollectionDate = nextDate,
                            EstimatedTime = currentTime,
                            Actual_Time = new TimeOnly(0, 0),
                            ConfirmImages = new List<string>(),
                            LicensePlate = vehicle.Plate_Number,
                            Status = "Đang lên lịch"
                        };

                        routes.Add(route);

                        routeDetails.Add(new RouteDetail
                        {
                            PickupOrder = order++,
                            PostId = x.Post.Id.GetHashCode(),
                            UserName = x.User?.Name ?? "Không rõ",
                            DistanceKm = Math.Round(x.Distance, 2),
                            Address = x.Post.Address,
                            Schedule = x.Post.ScheduleJson ?? "",
                            EstimatedArrival = currentTime.ToString("HH:mm")
                        });

                        currentTime = currentTime.AddMinutes(20);
                        if (currentTime > maxEnd)
                            currentTime = maxEnd;
                    }

                    if (request.SaveResult)
                    {
                        FakeDataSeeder.collectionGroups.Add(group);
                        FakeDataSeeder.collectionRoutes.AddRange(routes);
                    }

                    response.CreatedGroups.Add(new GroupSummary
                    {
                        GroupCode = group.Group_Code,
                        ShiftId = shift.s.Id,
                        Vehicle = $"{vehicle.Plate_Number} - {vehicle.Vehicle_Type} ({radius}km)",
                        Collector = FakeDataSeeder.collector.Name,
                        TotalPosts = routes.Count,
                        Routes = routeDetails
                    });
                }
            }

            return await Task.FromResult(response);
        }
    }
}
