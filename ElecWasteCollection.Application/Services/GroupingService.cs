using System.Text.Json;
using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Application.Interfaces;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.GroupModel;
using ElecWasteCollection.Domain.Entities;
using static ElecWasteCollection.Application.Data.FakeDataSeeder;

namespace ElecWasteCollection.Application.Services
{
    public class GroupingService : IGroupingService
    {

        private const double SERVICE_TIME_MINUTES = 15;
        private const double AVG_TRAVEL_MINUTES = 15;
        private const double SPEED_KM_H_LARGE = 30;
        private const double SPEED_KM_H_SMALL = 25;

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

        private class PostScheduleInfo
        {
            public DateOnly MinDate { get; set; }
            public DateOnly MaxDate { get; set; }
            public List<DateOnly> SpecificDates { get; set; } = new();
        }

        private (double length, double width, double height, double weight, double volume, string dimensionText)
    GetProductAttributes(Guid productId)
        {
            var map = FakeDataSeeder.productValues
                .Where(v => v.ProductId == productId)
                .ToDictionary(v => v.AttributeId, v => v.Value);

            double length = map.GetValueOrDefault(Guid.Parse("a1a1a1a1-0002-0002-0002-000000000001"), 0);
            double width = map.GetValueOrDefault(Guid.Parse("a1a1a1a1-0002-0002-0002-000000000002"), 0);
            double height = map.GetValueOrDefault(Guid.Parse("a1a1a1a1-0002-0002-0002-000000000003"), 0);
            double weight = map.GetValueOrDefault(Guid.Parse("a1a1a1a1-0009-0009-0009-000000000001"), 0);
            double volume = map.GetValueOrDefault(Guid.Parse("a1a1a1a1-0004-0004-0004-000000000001"), 0);

            string dim = $"{length} x {width} x {height} cm";

            return (length, width, height, weight, volume, dim);
        }

        private static bool TryParseScheduleInfo(string raw, out PostScheduleInfo info)
        {
            info = new PostScheduleInfo();
            if (string.IsNullOrWhiteSpace(raw)) return false;

            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var days = JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>(raw, opts);
                if (days == null || !days.Any()) return false;

                var validDates = new List<DateOnly>();

                foreach (var d in days)
                {
                    if (DateOnly.TryParse(d.PickUpDate, out var date) &&
                        d.Slots != null &&
                        TimeOnly.TryParse(d.Slots.StartTime, out var s) &&
                        TimeOnly.TryParse(d.Slots.EndTime, out var e) &&
                        s < e)
                    {
                        validDates.Add(date);
                    }
                }

                if (!validDates.Any()) return false;

                validDates.Sort();
                info.SpecificDates = validDates;
                info.MinDate = validDates.First();
                info.MaxDate = validDates.Last();

                return true;
            }
            catch
            {
                return false;
            }
        }
        private static bool TryGetTimeWindowForDate(string raw, DateOnly target, out TimeOnly start, out TimeOnly end)
        {
            start = TimeOnly.MinValue;
            end = TimeOnly.MaxValue;

            if (string.IsNullOrWhiteSpace(raw)) return false;

            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var days = JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>(raw, opts);
                var match = days?.FirstOrDefault(d =>
                    DateOnly.TryParse(d.PickUpDate, out var date) && date == target);

                if (match?.Slots != null &&
                    TimeOnly.TryParse(match.Slots.StartTime, out var s) &&
                    TimeOnly.TryParse(match.Slots.EndTime, out var e))
                {
                    start = s;
                    end = e;
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<PreAssignResponse> PreAssignAsync(PreAssignRequest request)
        {
            var point = FakeDataSeeder.smallCollectionPoints.FirstOrDefault(p => p.Id == request.CollectionPointId)
                ?? throw new Exception("Không tìm thấy trạm thu gom.");

            double maxRadius = FakeDataSeeder.vehicles.Max(v => v.Radius_Km);

            var rawPosts = FakeDataSeeder.posts.Where(p =>
            {
                var prod = FakeDataSeeder.products.FirstOrDefault(x => x.Id == p.ProductId);
                return prod != null && prod.Status == "Chờ gom nhóm";
            }).ToList();

            if (!rawPosts.Any())
                throw new Exception("Không có bài đăng nào cần xử lý.");

            var pool = new List<dynamic>();

            foreach (var p in rawPosts)
            {
                if (TryParseScheduleInfo(p.ScheduleJson!, out var sch))
                {
                    var user = FakeDataSeeder.users.First(u => u.UserId == p.SenderId);
                    var attributes = GetProductAttributes(p.ProductId);

                    double userLat = user?.Iat ?? point.Latitude;
                    double userLng = user?.Ing ?? point.Longitude;

                    double dist = GeoHelper.DistanceKm(point.Latitude, point.Longitude, userLat, userLng);

                    if (dist > maxRadius) continue;

                    pool.Add(new
                    {
                        Post = p,
                        Schedule = sch,
                        Length = attributes.length,
                        Width = attributes.width,
                        Height = attributes.height,
                        DimensionText = attributes.dimensionText,
                        Weight = attributes.weight,
                        Volume = attributes.volume,
                        UserName = user?.Name,
                        Address = user?.Address
                    });
                }
            }

            var distinctDates = pool
                .SelectMany(x => (List<DateOnly>)x.Schedule.SpecificDates)
                .Distinct()
                .Where(d => d >= DateOnly.FromDateTime(DateTime.Now))
                .OrderBy(d => d)
                .ToList();

            var response = new PreAssignResponse
            {
                CollectionPoint = point.Name,
                LoadThresholdPercent = request.LoadThresholdPercent
            };

            foreach (var date in distinctDates)
            {
                var candidates = pool
                    .Where(x => ((List<DateOnly>)x.Schedule.SpecificDates).Contains(date))
                    .OrderBy(x => x.Schedule.MaxDate)
                    .ThenBy(x => x.Schedule.MinDate)
                    .ToList();

                if (!candidates.Any()) continue;

                var shiftsToday = FakeDataSeeder.shifts.Where(s => s.WorkDate == date).ToList();

                double totalWorkMinutes = shiftsToday.Any()
                    ? shiftsToday.Sum(s => (s.Shift_End_Time - s.Shift_Start_Time).TotalMinutes)
                    : 8 * 60;

                double estimatedMinutesPerPost = SERVICE_TIME_MINUTES + AVG_TRAVEL_MINUTES;
                int maxPostsCapacity = (int)(totalWorkMinutes / estimatedMinutesPerPost);

                var feasible = candidates.Take(maxPostsCapacity).ToList();

                double totalWeightNeed = feasible.Sum(x => (double)x.Weight);

                var availableVehicles = FakeDataSeeder.vehicles
                    .Where(v => v.Status == "active")
                    .OrderBy(v => v.Capacity_Kg)
                    .ToList();

                var suggestedVehicle = availableVehicles
                    .FirstOrDefault(v =>
                        totalWeightNeed <= v.Capacity_Kg * (request.LoadThresholdPercent / 100.0))
                    ?? availableVehicles.Last();

                double allowedKg = suggestedVehicle.Capacity_Kg * (request.LoadThresholdPercent / 100.0);

                double curWeight = 0;
                var selectedPosts = new List<PreAssignPost>();
                var assigned = new List<dynamic>();

                foreach (var item in feasible)
                {
                    if (curWeight + item.Weight <= allowedKg)
                    {
                        curWeight += item.Weight;

                        selectedPosts.Add(new PreAssignPost
                        {
                            PostId = item.Post.Id,
                            UserName = item.UserName,
                            Address = item.Address,
                            Length = item.Length,
                            Width = item.Width,
                            Height = item.Height,
                            DimensionText = item.DimensionText,
                            Weight = item.Weight,
                            Volume = item.Volume
                        });

                        assigned.Add(item);
                    }
                }

                foreach (var a in assigned) pool.Remove(a);

                if (selectedPosts.Any())
                {
                    response.Days.Add(new PreAssignDay
                    {
                        WorkDate = date,
                        OriginalPostCount = selectedPosts.Count,
                        TotalWeight = Math.Round(selectedPosts.Sum(x => x.Weight), 2),
                        TotalVolume = Math.Round(selectedPosts.Sum(x => x.Volume), 2),
                        SuggestedVehicle = new SuggestedVehicle
                        {
                            Id = suggestedVehicle.Id,
                            Plate_Number = suggestedVehicle.Plate_Number,
                            Vehicle_Type = suggestedVehicle.Vehicle_Type,
                            Capacity_Kg = suggestedVehicle.Capacity_Kg,
                            AllowedCapacityKg = allowedKg
                        },
                        Posts = selectedPosts
                    });
                }

                if (!pool.Any()) break;
            }

            return await Task.FromResult(response);
        }

        public async Task<bool> AssignDayAsync(AssignDayRequest request)
        {
            var vehicle = FakeDataSeeder.vehicles.FirstOrDefault(v => v.Id == request.VehicleId)
                ?? throw new Exception("Xe không tồn tại.");

            if (!FakeDataSeeder.smallCollectionPoints.Any(p => p.Id == request.CollectionPointId))
                throw new Exception("Trạm không tồn tại.");

            if (request.PostIds == null || !request.PostIds.Any())
                throw new Exception("Danh sách bài đăng trống.");

            bool busy = FakeDataSeeder.stagingAssignDays.Any(s =>
                s.Date == request.WorkDate &&
                s.VehicleId == request.VehicleId &&
                s.PointId != request.CollectionPointId);

            if (busy)
                throw new Exception($"Xe {vehicle.Plate_Number} đã được điều động nơi khác.");

            FakeDataSeeder.stagingAssignDays.RemoveAll(s =>
                s.Date == request.WorkDate &&
                s.PointId == request.CollectionPointId);

            FakeDataSeeder.stagingAssignDays.Add(new StagingAssignDay
            {
                Date = request.WorkDate,
                PointId = request.CollectionPointId,
                VehicleId = request.VehicleId,
                PostIds = request.PostIds
            });

            return await Task.FromResult(true);
        }

        public async Task<GroupingByPointResponse> GroupByCollectionPointAsync(GroupingByPointRequest request)
        {
            var point = FakeDataSeeder.smallCollectionPoints
                .FirstOrDefault(p => p.Id == request.CollectionPointId)
                ?? throw new Exception("Không tìm thấy trạm.");

            var staging = FakeDataSeeder.stagingAssignDays
                .Where(s => s.PointId == request.CollectionPointId)
                .OrderBy(s => s.Date)
                .ToList();

            if (!staging.Any())
                throw new Exception("Chưa có dữ liệu Assign. Hãy chạy AssignDay trước.");

            var response = new GroupingByPointResponse
            {
                CollectionPoint = point.Name,
                SavedToDatabase = request.SaveResult
            };

            int groupCounter = 1;

            foreach (var assignDay in staging)
            {
                var workDate = assignDay.Date;

                var posts = FakeDataSeeder.posts
                    .Where(p => assignDay.PostIds.Contains(p.Id))
                    .ToList();

                if (!posts.Any()) continue;

                var shifts = FakeDataSeeder.shifts
                    .Where(s => s.WorkDate == workDate)
                    .OrderBy(s => s.Shift_Start_Time)
                    .ToList();

                if (!shifts.Any())
                    throw new Exception($"Ngày {workDate} chưa có Ca làm việc.");

                var vehicle = FakeDataSeeder.vehicles
                    .FirstOrDefault(v => v.Id == assignDay.VehicleId)
                    ?? throw new Exception($"Xe {assignDay.VehicleId} không tồn tại.");

                var oldRoutes = FakeDataSeeder.collectionRoutes
                    .Where(r => r.CollectionDate == workDate &&
                                assignDay.PostIds.Contains(r.PostId))
                    .ToList();

                var oldGroupIds = oldRoutes
                    .Select(r => r.CollectionGroupId)
                    .Distinct()
                    .ToList();

                foreach (var gid in oldGroupIds)
                {
                    FakeDataSeeder.collectionRoutes.RemoveAll(r => r.CollectionGroupId == gid);

                    var gObj = FakeDataSeeder.collectionGroups.FirstOrDefault(g => g.Id == gid);
                    if (gObj != null)
                        FakeDataSeeder.collectionGroups.Remove(gObj);
                }

                int? reuseGroupId = oldGroupIds.Any() ? oldGroupIds.Min() : null;
                bool reusedFirst = false;

                var unassigned = new List<dynamic>();

                foreach (var p in posts)
                {
                    var user = FakeDataSeeder.users.First(u => u.UserId == p.SenderId);

                    var attributes = GetProductAttributes(p.ProductId);

                    if (TryGetTimeWindowForDate(p.ScheduleJson!, workDate, out var start, out var end))
                    {
                        unassigned.Add(new
                        {
                            Post = p,
                            User = user,
                            Start = start,
                            End = end,

                            Length = attributes.length,
                            Width = attributes.width,
                            Height = attributes.height,
                            DimensionText = attributes.dimensionText,

                            Weight = attributes.weight,
                            Volume = attributes.volume,

                            Lat = user.Iat ?? point.Latitude,
                            Lng = user.Ing ?? point.Longitude
                        });
                    }
                }

                if (!unassigned.Any())
                    continue;

                foreach (var shift in shifts)
                {
                    if (!unassigned.Any())
                        break;

                    var collector = FakeDataSeeder.collectors
                        .FirstOrDefault(c => c.CollectorId == shift.CollectorId);

                    double speed = (vehicle.Vehicle_Type.Contains("lớn")
                        ? SPEED_KM_H_LARGE
                        : SPEED_KM_H_SMALL) / 60.0;

                    double curKg = 0, curM3 = 0;
                    double curLat = point.Latitude, curLng = point.Longitude;

                    TimeOnly cursor = TimeOnly.FromDateTime(shift.Shift_Start_Time);
                    var shiftEnd = TimeOnly.FromDateTime(shift.Shift_End_Time);

                    var routeNodes = new List<RouteDetail>();
                    var routeDbList = new List<CollectionRoutes>();

                    while (unassigned.Any())
                    {
                        var best = unassigned
                            .Select(node =>
                            {
                                double dist = GeoHelper.DistanceKm(curLat, curLng, node.Lat, node.Lng);
                                double travel = dist / speed;

                                TimeOnly arrival = cursor.AddMinutes(travel);
                                TimeOnly actualStart = arrival < node.Start ? node.Start : arrival;

                                bool ok =
                                    (curKg + node.Weight <= vehicle.Capacity_Kg) &&
                                    (curM3 + node.Volume <= vehicle.Capacity_M3) &&
                                    (actualStart <= node.End) &&
                                    (actualStart.AddMinutes(SERVICE_TIME_MINUTES) <= shiftEnd);

                                return new
                                {
                                    Node = node,
                                    Dist = Math.Round(dist, 2),
                                    ActualStart = actualStart,
                                    Valid = ok
                                };
                            })
                            .Where(x => x.Valid)
                            .OrderBy(x => x.ActualStart)
                            .ThenBy(x => x.Dist)
                            .FirstOrDefault();

                        if (best == null)
                            break;

                        var chosen = best.Node;

                        routeNodes.Add(new RouteDetail
                        {
                            PickupOrder = routeNodes.Count + 1,
                            PostId = chosen.Post.Id,
                            UserName = chosen.User.Name,
                            Address = chosen.User.Address,
                            DistanceKm = best.Dist,
                            Schedule = chosen.Post.ScheduleJson,
                            EstimatedArrival = best.ActualStart.ToString("HH:mm"),

                            Length = chosen.Length,
                            Width = chosen.Width,
                            Height = chosen.Height,
                            DimensionText = chosen.DimensionText,
                            WeightKg = chosen.Weight,
                            VolumeM3 = chosen.Volume
                        });

                        routeDbList.Add(new CollectionRoutes
                        {
                            CollectionRouteId = Guid.NewGuid(),
                            PostId = chosen.Post.Id,
                            CollectionDate = workDate,
                            EstimatedTime = best.ActualStart,
                            Status = "Chưa bắt đầu",
                            DistanceKm = best.Dist
                        });

                        curKg += chosen.Weight;
                        curM3 += chosen.Volume;

                        curLat = chosen.Lat;
                        curLng = chosen.Lng;
                        cursor = best.ActualStart.AddMinutes(SERVICE_TIME_MINUTES);

                        unassigned.Remove(chosen);
                    }

                    if (!routeNodes.Any()) continue;

                    int newGroupId;
                    string newGroupCode;

                    if (!reusedFirst && reuseGroupId.HasValue)
                    {
                        newGroupId = reuseGroupId.Value;
                        newGroupCode = $"GRP-{workDate:MMdd}-1";
                        reusedFirst = true;
                    }
                    else
                    {
                        newGroupId = FakeDataSeeder.collectionGroups.Count + 1;
                        newGroupCode = $"GRP-{workDate:MMdd}-{groupCounter++}";
                    }

                    var group = new CollectionGroups
                    {
                        Id = newGroupId,
                        Group_Code = newGroupCode,
                        Name = $"{vehicle.Vehicle_Type} - {vehicle.Plate_Number}",
                        Shift_Id = shift.Id,
                        Created_At = DateTime.Now
                    };

                    foreach (var rt in routeDbList)
                        rt.CollectionGroupId = group.Id;

                    if (request.SaveResult)
                    {
                        FakeDataSeeder.collectionGroups.Add(group);
                        FakeDataSeeder.collectionRoutes.AddRange(routeDbList);
                    }

                    response.CreatedGroups.Add(new GroupSummary
                    {
                        GroupCode = group.Group_Code,
                        GroupId = group.Id,
                        ShiftId = shift.Id,
                        Vehicle = $"{vehicle.Plate_Number} ({vehicle.Vehicle_Type})",
                        Collector = collector?.Name ?? "Unknown",
                        GroupDate = workDate,
                        TotalPosts = routeNodes.Count,
                        TotalWeightKg = Math.Round(curKg, 2),
                        TotalVolumeM3 = Math.Round(curM3, 2),
                        Routes = routeNodes
                    });
                }
            }

            return await Task.FromResult(response);
        }


        public async Task<object> GetRoutesByGroupAsync(int groupId)
        {
            var group = FakeDataSeeder.collectionGroups
                .FirstOrDefault(g => g.Id == groupId)
                ?? throw new Exception("Không tìm thấy group.");

            var shift = FakeDataSeeder.shifts.First(s => s.Id == group.Shift_Id);

            var routes = FakeDataSeeder.collectionRoutes
                .Where(r => r.CollectionGroupId == groupId)
                .OrderBy(r => r.EstimatedTime)
                .ToList();

            if (!routes.Any())
                throw new Exception("Group không có routes.");

            var workDate = shift.WorkDate;
            var firstPostId = routes.First().PostId;

            var staging = FakeDataSeeder.stagingAssignDays
                .First(s => s.Date == workDate && s.PostIds.Contains(firstPostId));

            var point = FakeDataSeeder.smallCollectionPoints
                .First(p => p.Id == staging.PointId);

            var vehicle = FakeDataSeeder.vehicles.First(v => v.Id == staging.VehicleId);
            var collector = FakeDataSeeder.collectors.FirstOrDefault(c => c.CollectorId == shift.CollectorId);

            double totalWeight = 0, totalVolume = 0;
            int pickup = 1;

            var routeList = new List<object>();

            foreach (var r in routes)
            {
                var post = FakeDataSeeder.posts.First(p => p.Id == r.PostId);
                var user = FakeDataSeeder.users.First(u => u.UserId == post.SenderId);

                var att = GetProductAttributes(post.ProductId);

                totalWeight += att.weight;
                totalVolume += att.volume;

                routeList.Add(new
                {
                    pickupOrder = pickup++,
                    postId = post.Id,
                    userName = user.Name,
                    address = user.Address,

                    length = att.length,
                    width = att.width,
                    height = att.height,
                    dimensionText = att.dimensionText,

                    weightKg = att.weight,
                    volumeM3 = att.volume,

                    distanceKm = r.DistanceKm,
                    schedule = post.ScheduleJson,
                    estimatedArrival = r.EstimatedTime.ToString("HH:mm")
                });
            }

            var result = new
            {
                groupId = group.Id,
                groupCode = group.Group_Code,
                shiftId = group.Shift_Id,
                vehicle = $"{vehicle.Plate_Number} ({vehicle.Vehicle_Type})",
                collector = collector?.Name ?? "Unknown",
                groupDate = workDate.ToString("yyyy-MM-dd"),
                collectionPoint = point.Name,
                totalPosts = routes.Count,
                totalWeightKg = Math.Round(totalWeight, 2),
                totalVolumeM3 = Math.Round(totalVolume, 2),
                routes = routeList
            };

            return await Task.FromResult(result);
        }

        public async Task<List<object>> GetGroupsByPointIdAsync(int pointId)
        {
            var stagings = FakeDataSeeder.stagingAssignDays
                .Where(s => s.PointId == pointId)
                .ToList();

            if (!stagings.Any())
                throw new Exception("CollectionPoint chưa có Group nào.");

            var groupIds = new HashSet<int>();

            foreach (var st in stagings)
            {
                var routes = FakeDataSeeder.collectionRoutes
                    .Where(r => r.CollectionDate == st.Date &&
                                st.PostIds.Contains(r.PostId))
                    .ToList();

                foreach (var rt in routes)
                    groupIds.Add(rt.CollectionGroupId);
            }

            var result = new List<object>();

            foreach (var gid in groupIds)
            {
                var group = FakeDataSeeder.collectionGroups.First(g => g.Id == gid);
                var shift = FakeDataSeeder.shifts.First(s => s.Id == group.Shift_Id);

                var firstRoute = FakeDataSeeder.collectionRoutes.First(r => r.CollectionGroupId == gid);
                var staging = FakeDataSeeder.stagingAssignDays.First(s =>
                    s.Date == shift.WorkDate && s.PostIds.Contains(firstRoute.PostId));

                var vehicle = FakeDataSeeder.vehicles.First(v => v.Id == staging.VehicleId);
                var collector = FakeDataSeeder.collectors.FirstOrDefault(c => c.CollectorId == shift.CollectorId);

                var routes = FakeDataSeeder.collectionRoutes.Where(r => r.CollectionGroupId == gid).ToList();

                double totalWeight = 0, totalVolume = 0;

                foreach (var r in routes)
                {
                    var post = FakeDataSeeder.posts.First(p => p.Id == r.PostId);
                    var att = GetProductAttributes(post.ProductId);

                    totalWeight += att.weight;
                    totalVolume += att.volume;
                }

                result.Add(new
                {
                    groupId = group.Id,
                    groupCode = group.Group_Code,
                    shiftId = group.Shift_Id,
                    vehicle = $"{vehicle.Plate_Number} ({vehicle.Vehicle_Type})",
                    collector = collector?.Name ?? "Unknown",
                    groupDate = shift.WorkDate.ToString("yyyy-MM-dd"),
                    totalPosts = routes.Count,
                    totalWeightKg = Math.Round(totalWeight, 2),
                    totalVolumeM3 = Math.Round(totalVolume, 2)
                });
            }

            return await Task.FromResult(result);
        }

        public async Task<List<Vehicles>> GetVehiclesAsync()
        {
            return await Task.FromResult(
                FakeDataSeeder.vehicles
                    .Where(v => v.Status == "active")
                    .OrderBy(v => v.Id)
                    .ToList()
            );
        }

        public async Task<List<PendingPostModel>> GetPendingPostsAsync()
        {
            var posts = FakeDataSeeder.posts
                .Where(p =>
                    FakeDataSeeder.products.Any(pr =>
                        pr.Id == p.ProductId &&
                        pr.Status == "Chờ gom nhóm"))
                .ToList();

            var list = new List<PendingPostModel>();

            foreach (var p in posts)
            {
                var user = FakeDataSeeder.users.First(u => u.UserId == p.SenderId);
                var prod = FakeDataSeeder.products.First(pr => pr.Id == p.ProductId);

                var brand = FakeDataSeeder.brands.First(b => b.BrandId == prod.BrandId);
                var cat = FakeDataSeeder.categories.First(c => c.Id == prod.CategoryId);

                var a = GetProductAttributes(p.ProductId);

                list.Add(new PendingPostModel
                {
                    PostId = p.Id,
                    UserName = user.Name,
                    Address = user.Address,

                    ProductName = $"{brand.Name} {cat.Name}",

                    Length = a.length,
                    Width = a.width,
                    Height = a.height,
                    DimensionText = a.dimensionText,

                    Weight = a.weight,
                    Volume = a.volume,

                    ScheduleJson = p.ScheduleJson!,
                    Status = prod.Status
                });
            }

            return await Task.FromResult(list);
        }
    }
}
