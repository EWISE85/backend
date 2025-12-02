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

        private readonly List<UserAddress> _userAddress = FakeDataSeeder.userAddress;

        private readonly Guid _attTrongLuong = FakeDataSeeder.ID_TrongLuong;
        private readonly Guid _attKhoiLuongGiat = FakeDataSeeder.ID_KhoiLuongGiat;

        private readonly Guid _attChieuDai = FakeDataSeeder.ID_ChieuDai;
        private readonly Guid _attChieuRong = FakeDataSeeder.ID_ChieuRong;
        private readonly Guid _attChieuCao = FakeDataSeeder.ID_ChieuCao;

        private readonly Guid _attDungTich = FakeDataSeeder.ID_DungTich;
        private readonly Guid _attKichThuocManHinh = FakeDataSeeder.ID_KichThuocManHinh;

        private sealed class TimeSlotDetailDto { public string? StartTime { get; set; } public string? EndTime { get; set; } }
        private sealed class DailyTimeSlotsDto { public string? DayName { get; set; } public string? PickUpDate { get; set; } public TimeSlotDetailDto? Slots { get; set; } }
        private class PostScheduleInfo { public DateOnly MinDate { get; set; } public DateOnly MaxDate { get; set; } public List<DateOnly> SpecificDates { get; set; } = new(); }

        private (double length, double width, double height, double weight, double volume, string dimensionText)
          GetProductAttributes(Guid productId)
        {
            var pValues = FakeDataSeeder.productValues.Where(v => v.ProductId == productId).ToList();
            var allOptions = FakeDataSeeder.attributeOptions;

            double weight = 0;

            var weightAttributeIds = new[] { _attTrongLuong, _attKhoiLuongGiat, _attDungTich };

            foreach (var attId in weightAttributeIds)
            {
                var pVal = pValues.FirstOrDefault(v => v.AttributeId == attId);

                if (pVal != null && pVal.AttributeOptionId.HasValue)
                {
                    var opt = allOptions.FirstOrDefault(o => o.OptionId == pVal.AttributeOptionId);

                    if (opt != null && opt.EstimateWeight.HasValue && opt.EstimateWeight.Value > 0)
                    {
                        weight = opt.EstimateWeight.Value;
                        break;
                    }
                }
            }

            if (weight <= 0) weight = 1; 

            double length = pValues.FirstOrDefault(v => v.AttributeId == _attChieuDai)?.Value ?? 0;
            double width = pValues.FirstOrDefault(v => v.AttributeId == _attChieuRong)?.Value ?? 0;
            double height = pValues.FirstOrDefault(v => v.AttributeId == _attChieuCao)?.Value ?? 0;

            double volume = 0;
            string dimText = "";

            if (length > 0 && width > 0 && height > 0)
            {
                volume = length * width * height; 
                dimText = $"{length} x {width} x {height} cm";
            }
            else
            {
                var volumeAttributeIds = new[] { _attKichThuocManHinh, _attDungTich, _attKhoiLuongGiat, _attTrongLuong };

                foreach (var attId in volumeAttributeIds)
                {
                    var pVal = pValues.FirstOrDefault(v => v.AttributeId == attId);

                    if (pVal != null && pVal.AttributeOptionId.HasValue)
                    {
                        var opt = allOptions.FirstOrDefault(o => o.OptionId == pVal.AttributeOptionId);
                        if (opt != null && opt.EstimateVolume.HasValue && opt.EstimateVolume.Value > 0)
                        {
                            volume = opt.EstimateVolume.Value * 1_000_000;
                            dimText = $"~ {opt.OptionName}";
                            break;
                        }
                    }
                }
            }

            if (volume <= 0)
            {
                volume = 1000;
                dimText = "Không xác định";
            }

            return (length, width, height, weight, volume, dimText);
        }

        private static bool TryParseScheduleInfo(string raw, out PostScheduleInfo info)
        {
            info = new PostScheduleInfo();
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var days = JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>(raw, opts);
                if (days == null || !days.Any())
                    return false;

                var valid = new List<DateOnly>();
                foreach (var d in days)
                {
                    if (DateOnly.TryParse(d.PickUpDate, out var date) &&
                        d.Slots != null &&
                        TimeOnly.TryParse(d.Slots.StartTime, out var s) &&
                        TimeOnly.TryParse(d.Slots.EndTime, out var e) &&
                        s < e)
                    {
                        valid.Add(date);
                    }
                }

                if (!valid.Any())
                    return false;

                valid.Sort();
                info.SpecificDates = valid;
                info.MinDate = valid.First();
                info.MaxDate = valid.Last();
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
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var days = JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>(raw, opts);
                var match = days?.FirstOrDefault(d =>
                    DateOnly.TryParse(d.PickUpDate, out var dt) && dt == target);
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
                ?? throw new Exception("Không tìm thấy trạm.");

            double maxRadius = FakeDataSeeder.vehicles.Max(v => v.Radius_Km);

            var rawPosts = FakeDataSeeder.posts.Where(p =>
            {
                if (p.AssignedSmallPointId != request.CollectionPointId) return false;
                var prod = FakeDataSeeder.products.FirstOrDefault(x => x.Id == p.ProductId);
                return prod != null && prod.Status == "Chờ gom nhóm";
            }).ToList();

            if (!rawPosts.Any()) throw new Exception("Không có bài đăng nào của trạm này.");

            var pool = new List<dynamic>();

            foreach (var p in rawPosts)
            {
                if (TryParseScheduleInfo(p.ScheduleJson!, out var sch))
                {
                    var user = FakeDataSeeder.users.First(u => u.UserId == p.SenderId);
                    var att = GetProductAttributes(p.ProductId);

                    var userAddress = _userAddress.FirstOrDefault(ua => ua.UserId == user.UserId);
                    double lat = userAddress?.Iat ?? point.Latitude;
                    double lng = userAddress?.Ing ?? point.Longitude;
                    double dist = GeoHelper.DistanceKm(point.Latitude, point.Longitude, lat, lng);

                    if (dist > maxRadius) continue;

                    pool.Add(new
                    {
                        Post = p,
                        Schedule = sch,
                        Length = att.length,
                        Width = att.width,
                        Height = att.height,
                        DimensionText = att.dimensionText,
                        Weight = att.weight,
                        Volume = att.volume,
                        UserName = user.Name,
                        Address = userAddress?.Address
                    });
                }
            }

            var distinctDates = pool.SelectMany(x => (List<DateOnly>)x.Schedule.SpecificDates).Distinct().OrderBy(x => x).ToList();

            var res = new PreAssignResponse { CollectionPoint = point.Name, LoadThresholdPercent = request.LoadThresholdPercent };

            foreach (var date in distinctDates)
            {
                var candidates = pool.Where(x => ((List<DateOnly>)x.Schedule.SpecificDates).Contains(date))
                    .OrderBy(x => x.Schedule.MaxDate).ThenBy(x => x.Schedule.MinDate).ToList();

                if (!candidates.Any()) continue;

                var shiftsToday = FakeDataSeeder.shifts.Where(s => s.WorkDate == date).ToList();
                double totalWorkMinutes = shiftsToday.Any() ? shiftsToday.Sum(s => (s.Shift_End_Time - s.Shift_Start_Time).TotalMinutes) : 8 * 60;
                double estimatedMinutesPerPost = SERVICE_TIME_MINUTES + AVG_TRAVEL_MINUTES;
                int maxPosts = (int)(totalWorkMinutes / estimatedMinutesPerPost);

                var feasible = candidates.Take(maxPosts).ToList();

                double totalWeightNeed = feasible.Sum(x => (double)x.Weight);
                double totalVolumeNeedM3 = feasible.Sum(x => (double)x.Volume) / 1_000_000.0;

                var vehicles = FakeDataSeeder.vehicles.Where(v => v.Status == "active").OrderBy(v => v.Capacity_Kg).ToList();

                var suggested = vehicles.FirstOrDefault(v =>
                    totalWeightNeed <= v.Capacity_Kg * (request.LoadThresholdPercent / 100.0) &&
                    totalVolumeNeedM3 <= v.Capacity_M3 * (request.LoadThresholdPercent / 100.0)
                ) ?? vehicles.Last();

                double ratio = request.LoadThresholdPercent / 100.0;
                double allowedKg = suggested.Capacity_Kg * ratio;
                double allowedM3 = suggested.Capacity_M3 * ratio;

                double curKg = 0;
                double curM3 = 0;
                var selected = new List<PreAssignPost>();
                var removeList = new List<dynamic>();

                foreach (var item in feasible)
                {
                    double itemM3 = item.Volume / 1_000_000.0;
                    if (curKg + item.Weight <= allowedKg && curM3 + itemM3 <= allowedM3)
                    {
                        curKg += item.Weight;
                        curM3 += itemM3;
                        selected.Add(new PreAssignPost
                        {
                            PostId = item.Post.Id,
                            ProductId = item.Post.ProductId,
                            UserName = item.UserName,
                            Address = item.Address,
                            Length = item.Length,
                            Width = item.Width,
                            Height = item.Height,
                            DimensionText = item.DimensionText,
                            Weight = item.Weight,
                            Volume = Math.Round(itemM3, 4)
                        });
                        removeList.Add(item);
                    }
                }

                foreach (var x in removeList) pool.Remove(x);

                if (selected.Any())
                {
                    res.Days.Add(new PreAssignDay
                    {
                        WorkDate = date,
                        OriginalPostCount = selected.Count,
                        TotalWeight = Math.Round(selected.Sum(x => x.Weight), 2),
                        TotalVolume = Math.Round(selected.Sum(x => x.Volume), 4),
                        SuggestedVehicle = new SuggestedVehicle { Id = suggested.Id, Plate_Number = suggested.Plate_Number, Vehicle_Type = suggested.Vehicle_Type, Capacity_Kg = suggested.Capacity_Kg, AllowedCapacityKg = allowedKg },
                        Posts = selected
                    });
                }
                if (!pool.Any()) break;
            }
            return await Task.FromResult(res);
        }

        public async Task<bool> AssignDayAsync(AssignDayRequest request)
        {
            var vehicle = FakeDataSeeder.vehicles.FirstOrDefault(v => v.Id == request.VehicleId)
                ?? throw new Exception("Xe không tồn tại.");

            if (!FakeDataSeeder.smallCollectionPoints.Any(p => p.Id == request.CollectionPointId))
                throw new Exception("Trạm không tồn tại.");

            if (request.ProductIds == null || !request.ProductIds.Any())
                throw new Exception("Danh sách Product trống.");

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
                ProductIds = request.ProductIds
            });

            return await Task.FromResult(true);
        }

        public async Task<GroupingByPointResponse> GroupByCollectionPointAsync(GroupingByPointRequest request)
        {
            var point = FakeDataSeeder.smallCollectionPoints.FirstOrDefault(p => p.Id == request.CollectionPointId) ?? throw new Exception("Không tìm thấy trạm.");
            var staging = FakeDataSeeder.stagingAssignDays.Where(s => s.PointId == request.CollectionPointId).OrderBy(s => s.Date).ToList();
            if (!staging.Any()) throw new Exception("Chưa có dữ liệu Assign. Hãy chạy AssignDay trước.");

            var response = new GroupingByPointResponse { CollectionPoint = point.Name, SavedToDatabase = request.SaveResult };
            int groupCounter = 1;

            foreach (var assignDay in staging)
            {
                var workDate = assignDay.Date;
                var posts = FakeDataSeeder.posts.Where(p => assignDay.ProductIds.Contains(p.ProductId)).ToList();
                if (!posts.Any()) continue;

                var shifts = FakeDataSeeder.shifts.Where(s => s.WorkDate == workDate).OrderBy(s => s.Shift_Start_Time).ToList();
                if (!shifts.Any()) throw new Exception($"Ngày {workDate} chưa có ca làm việc.");

                var vehicle = FakeDataSeeder.vehicles.First(v => v.Id == assignDay.VehicleId);

                var oldRoutes = FakeDataSeeder.collectionRoutes.Where(r => r.CollectionDate == workDate && assignDay.ProductIds.Contains(r.ProductId)).ToList();
                var oldGroupIds = oldRoutes.Select(r => r.CollectionGroupId).Distinct().ToList();
                foreach (var gid in oldGroupIds) { FakeDataSeeder.collectionRoutes.RemoveAll(r => r.CollectionGroupId == gid); FakeDataSeeder.collectionGroups.RemoveAll(g => g.Id == gid); }
                int? reuseGroupId = oldGroupIds.Any() ? oldGroupIds.Min() : null;
                bool reusedFirst = false;

                var unassigned = new List<dynamic>();

                foreach (var p in posts)
                {
                    var user = FakeDataSeeder.users.First(u => u.UserId == p.SenderId);
                    var att = GetProductAttributes(p.ProductId);
                    var address = _userAddress.FirstOrDefault(a => a.UserId == p.SenderId);
                    if (address == null) continue;

                    if (TryGetTimeWindowForDate(p.ScheduleJson!, workDate, out var st, out var en))
                    {
                        unassigned.Add(new
                        {
                            Post = p,
                            User = user,
                            Start = st,
                            End = en,
                            Length = att.length,
                            Width = att.width,
                            Height = att.height,
                            DimensionText = att.dimensionText,
                            Weight = att.weight,
                            Volume = att.volume / 1_000_000.0,
                            Lat = address.Iat ?? point.Latitude,
                            Lng = address.Ing ?? point.Longitude,
                            Address = address.Address
                        });
                    }
                }

                if (!unassigned.Any()) continue;

                foreach (var shift in shifts)
                {
                    if (!unassigned.Any()) break;
                    var collector = FakeDataSeeder.users.FirstOrDefault(u => u.UserId == shift.CollectorId);
                    double speed = (vehicle.Vehicle_Type.Contains("lớn") ? SPEED_KM_H_LARGE : SPEED_KM_H_SMALL) / 60.0;

                    double curKg = 0, curM3 = 0;
                    double curLat = point.Latitude, curLng = point.Longitude;
                    TimeOnly cursor = TimeOnly.FromDateTime(shift.Shift_Start_Time);
                    var shiftEnd = TimeOnly.FromDateTime(shift.Shift_End_Time);

                    var routeNodes = new List<RouteDetail>();
                    var saveRoutes = new List<CollectionRoutes>();

                    while (unassigned.Any())
                    {
                        var best = unassigned.Select(n =>
                        {
                            double dist = GeoHelper.DistanceKm(curLat, curLng, n.Lat, n.Lng);
                            double travel = dist / speed;
                            TimeOnly arr = cursor.AddMinutes(travel);
                            TimeOnly actual = arr < n.Start ? n.Start : arr;

                            bool ok = (curKg + n.Weight <= vehicle.Capacity_Kg) && (curM3 + n.Volume <= vehicle.Capacity_M3) &&
                                      actual <= n.End && actual.AddMinutes(SERVICE_TIME_MINUTES) <= shiftEnd;

                            return new { Node = n, Dist = Math.Round(dist, 2), Arrival = actual, Valid = ok };
                        })
                        .Where(x => x.Valid).OrderBy(x => x.Arrival).ThenBy(x => x.Dist).FirstOrDefault();

                        if (best == null) break;

                        var chosen = best.Node;
                        routeNodes.Add(new RouteDetail
                        {
                            PickupOrder = routeNodes.Count + 1,
                            ProductId = chosen.Post.ProductId,
                            UserName = chosen.User.Name,
                            Address = chosen.Address,
                            DistanceKm = best.Dist,
                            Schedule = JsonSerializer.Deserialize<object>(chosen.Post.ScheduleJson!),
                            EstimatedArrival = best.Arrival.ToString("HH:mm"),
                            Length = chosen.Length,
                            Width = chosen.Width,
                            Height = chosen.Height,
                            DimensionText = chosen.DimensionText,
                            WeightKg = chosen.Weight,
                            VolumeM3 = chosen.Volume
                        });

                        saveRoutes.Add(new CollectionRoutes { CollectionRouteId = Guid.NewGuid(), ProductId = chosen.Post.ProductId, CollectionDate = workDate, EstimatedTime = best.Arrival, DistanceKm = best.Dist, Status = "Chưa bắt đầu" });

                        curKg += chosen.Weight; curM3 += chosen.Volume; curLat = chosen.Lat; curLng = chosen.Lng;
                        cursor = best.Arrival.AddMinutes(SERVICE_TIME_MINUTES);
                        unassigned.Remove(chosen);
                    }

                    if (!routeNodes.Any()) continue;

                    int newGroupId;
                    string newGroupCode;
                    if (!reusedFirst && reuseGroupId.HasValue) { newGroupId = reuseGroupId.Value; newGroupCode = $"GRP-{workDate:MMdd}-1"; reusedFirst = true; }
                    else { newGroupId = FakeDataSeeder.collectionGroups.Count + 1; newGroupCode = $"GRP-{workDate:MMdd}-{groupCounter++}"; }

                    var group = new CollectionGroups { Id = newGroupId, Group_Code = newGroupCode, Shift_Id = shift.Id, Name = $"{vehicle.Vehicle_Type} - {vehicle.Plate_Number}", Created_At = DateTime.Now };
                    foreach (var rt in saveRoutes) rt.CollectionGroupId = group.Id;

                    if (request.SaveResult) { FakeDataSeeder.collectionGroups.Add(group); FakeDataSeeder.collectionRoutes.AddRange(saveRoutes); }

                    response.CreatedGroups.Add(new GroupSummary { GroupId = group.Id, GroupCode = group.Group_Code, Collector = collector?.Name ?? "Unknown", Vehicle = $"{vehicle.Plate_Number} ({vehicle.Vehicle_Type})", ShiftId = shift.Id, GroupDate = workDate, TotalPosts = routeNodes.Count, TotalWeightKg = Math.Round(curKg, 2), TotalVolumeM3 = Math.Round(curM3, 3), Routes = routeNodes });
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
                throw new Exception("Group không có route nào.");

            var workDate = shift.WorkDate;

            var firstProductId = routes.First().ProductId;

            var staging = FakeDataSeeder.stagingAssignDays
                .First(s => s.Date == workDate && s.ProductIds.Contains(firstProductId));

            var point = FakeDataSeeder.smallCollectionPoints
                .First(p => p.Id == staging.PointId);

            var vehicle = FakeDataSeeder.vehicles
                .First(v => v.Id == staging.VehicleId);

            var collector = FakeDataSeeder.users
                .FirstOrDefault(c => c.UserId == shift.CollectorId);

            double totalWeight = 0, totalVolume = 0;

            int order = 1;
            var routeList = new List<object>();

            foreach (var r in routes)
            {
                var post = FakeDataSeeder.posts.FirstOrDefault(p => p.ProductId == r.ProductId)
                    ?? throw new Exception("Không tìm thấy Post cho Product"); var user = FakeDataSeeder.users.First(u => u.UserId == post.SenderId);

                var userAddress = _userAddress.First(a => a.UserId == user.UserId);

                var att = GetProductAttributes(post.ProductId);

                totalWeight += att.weight;
                totalVolume += att.volume;

                routeList.Add(new
                {
                    pickupOrder = order++,
                    productId = post.ProductId,
                    postId = post.Id,
                    userName = user.Name,
                    address = userAddress.Address,
                    length = att.length,
                    width = att.width,
                    height = att.height,
                    dimensionText = att.dimensionText,
                    weightKg = att.weight,
                    volumeM3 = att.volume,
                    distanceKm = r.DistanceKm,
                    schedule = JsonSerializer.Deserialize<object>(post.ScheduleJson!),
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
                    .Where(r => r.CollectionDate == st.Date && st.ProductIds.Contains(r.ProductId))
                    .ToList();

                foreach (var rt in routes)
                    groupIds.Add(rt.CollectionGroupId);
            }

            var result = new List<object>();

            foreach (var gid in groupIds)
            {
                var group = FakeDataSeeder.collectionGroups.First(g => g.Id == gid);
                var shift = FakeDataSeeder.shifts.First(s => s.Id == group.Shift_Id);

                var routes = FakeDataSeeder.collectionRoutes
                    .Where(r => r.CollectionGroupId == gid)
                    .ToList();

                if (!routes.Any()) continue;

                var firstProductId = routes.First().ProductId;

                var staging = FakeDataSeeder.stagingAssignDays
                    .First(s => s.Date == shift.WorkDate &&
                                s.ProductIds.Contains(firstProductId));

                var vehicle = FakeDataSeeder.vehicles.First(v => v.Id == staging.VehicleId);

                var collector = FakeDataSeeder.users
                    .FirstOrDefault(c => c.UserId == shift.CollectorId);

                double totalWeight = 0, totalVolume = 0;

                foreach (var r in routes)
                {
                    var post = FakeDataSeeder.posts.First(p => p.ProductId == r.ProductId);
                    var att = GetProductAttributes(post.ProductId);

                    totalWeight += att.weight;
                    totalVolume += att.volume;
                }

                result.Add(new
                {
                    groupId = group.Id,
                    groupCode = group.Group_Code,
                    shiftId = group.Shift_Id,
                    groupDate = shift.WorkDate.ToString("yyyy-MM-dd"),
                    vehicle = $"{vehicle.Plate_Number} ({vehicle.Vehicle_Type})",
                    collector = collector?.Name ?? "Unknown",
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

            var result = new List<PendingPostModel>();

            foreach (var p in posts)
            {
                var user = FakeDataSeeder.users.First(u => u.UserId == p.SenderId);
                var product = FakeDataSeeder.products.First(pr => pr.Id == p.ProductId);
                var address = _userAddress.First(a => a.UserId == user.UserId);

                var brand = FakeDataSeeder.brands.First(b => b.BrandId == product.BrandId);
                var cat = FakeDataSeeder.categories.First(c => c.Id == product.CategoryId);

                var att = GetProductAttributes(p.ProductId);

                result.Add(new PendingPostModel
                {
                    PostId = p.Id,
                    ProductId = p.ProductId,
                    UserName = user.Name,
                    Address = address.Address,
                    ProductName = $"{brand.Name} {cat.Name}",
                    Length = att.length,
                    Width = att.width,
                    Height = att.height,
                    DimensionText = att.dimensionText,
                    Weight = att.weight,
                    Volume = att.volume,
                    ScheduleJson = p.ScheduleJson!,
                    Status = product.Status
                });
            }

            return await Task.FromResult(result);
        }
    }
}

