using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.Helpers;
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
        //private const double SPEED_KM_H_LARGE = 30;
        //private const double SPEED_KM_H_SMALL = 25;

        private readonly List<UserAddress> _userAddress = FakeDataSeeder.userAddress;

        private readonly Guid _attTrongLuong = FakeDataSeeder.ID_TrongLuong;
        private readonly Guid _attKhoiLuongGiat = FakeDataSeeder.ID_KhoiLuongGiat;

        private readonly Guid _attChieuDai = FakeDataSeeder.ID_ChieuDai;
        private readonly Guid _attChieuRong = FakeDataSeeder.ID_ChieuRong;
        private readonly Guid _attChieuCao = FakeDataSeeder.ID_ChieuCao;

        private readonly Guid _attDungTich = FakeDataSeeder.ID_DungTich;
        private readonly Guid _attKichThuocManHinh = FakeDataSeeder.ID_KichThuocManHinh;

        private readonly MapboxMatrixClient _matrixClient;
        public GroupingService(MapboxMatrixClient matrixClient)
        {
            _matrixClient = matrixClient;
        }

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

        private (double length, double width, double height, double weight, double volume, string dimensionText) GetProductAttributes(Guid productId)
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
                volume = (length * width * height) / 1_000_000.0;
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
                            volume = opt.EstimateVolume.Value;
                            dimText = $"~ {opt.OptionName}";
                            break;
                        }
                    }
                }
            }

            if (volume <= 0)
            {
                volume = 0.001;
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
            var point = FakeDataSeeder.smallCollectionPoints
                .FirstOrDefault(p => p.SmallCollectionPointsId == request.CollectionPointId)
                ?? throw new Exception("Không tìm thấy trạm thu gom.");

            var pointVehicles = FakeDataSeeder.vehicles
                .Where(v => v.Small_Collection_Point == request.CollectionPointId && v.Status == "active")
                .ToList();

            if (!pointVehicles.Any())
                throw new Exception("Trạm này hiện không có xe nào hoạt động.");

            double maxRadius = pointVehicles.Max(v => v.Radius_Km);

            var rawPosts = FakeDataSeeder.posts.Where(p =>
            {
                if (p.AssignedSmallPointId != null &&
                    p.AssignedSmallPointId != request.CollectionPointId)
                    return false;

                var prod = FakeDataSeeder.products.FirstOrDefault(x => x.ProductId == p.ProductId);
                return prod != null && prod.Status == "Chờ gom nhóm";
            }).ToList();

            if (!rawPosts.Any()) throw new Exception("Không có bài đăng nào phù hợp trong hệ thống.");

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
                        Address = userAddress?.Address ?? "Chưa cập nhật"
                    });
                }
            }

            if (!pool.Any()) throw new Exception("Không tìm thấy đơn hàng nào trong bán kính phục vụ.");

            var distinctDates = pool.SelectMany(x => (List<DateOnly>)x.Schedule.SpecificDates)
                                    .Distinct()
                                    .OrderBy(x => x)
                                    .ToList();

            var res = new PreAssignResponse
            {
                CollectionPoint = point.Name,
                LoadThresholdPercent = request.LoadThresholdPercent
            };

            foreach (var date in distinctDates)
            {
                var vehicleIdsInPoint = pointVehicles.Select(v => v.VehicleId).ToList();
                var shiftsToday = FakeDataSeeder.shifts
                    .Where(s => s.WorkDate == date && vehicleIdsInPoint.Contains(s.Vehicle_Id))
                    .ToList();

                if (!shiftsToday.Any()) continue;

                double totalWorkMinutes = shiftsToday.Sum(s => (s.Shift_End_Time - s.Shift_Start_Time).TotalMinutes);

                double estimatedMinutesPerPost = SERVICE_TIME_MINUTES + AVG_TRAVEL_MINUTES;

                int maxPostsByTime = (int)(totalWorkMinutes / estimatedMinutesPerPost);

                var candidates = pool
                    .Where(x => ((List<DateOnly>)x.Schedule.SpecificDates).Contains(date))
                    .OrderBy(x => x.Schedule.MaxDate)
                    .ThenBy(x => x.Schedule.MinDate)
                    .ToList();

                if (!candidates.Any()) continue;

                var feasibleTimeBound = candidates.Take(maxPostsByTime).ToList();

                var activeVehicleIds = shiftsToday.Select(s => s.Vehicle_Id).Distinct().ToList();
                var availableVehicles = pointVehicles
                    .Where(v => activeVehicleIds.Contains(v.VehicleId))
                    .OrderBy(v => v.Capacity_Kg)
                    .ToList();

                double totalWeightNeed = feasibleTimeBound.Sum(x => (double)x.Weight);
                double totalVolumeNeedM3 = feasibleTimeBound.Sum(x => (double)x.Volume);

                var suggested = availableVehicles.FirstOrDefault(v =>
                    totalWeightNeed <= v.Capacity_Kg * (request.LoadThresholdPercent / 100.0) &&
                    totalVolumeNeedM3 <= v.Capacity_M3 * (request.LoadThresholdPercent / 100.0)
                ) ?? availableVehicles.Last();

                double ratio = request.LoadThresholdPercent / 100.0;
                double allowedKg = suggested.Capacity_Kg * ratio;
                double allowedM3 = suggested.Capacity_M3 * ratio;

                double curKg = 0;
                double curM3 = 0;
                var selected = new List<PreAssignProduct>();
                var removeList = new List<dynamic>();

                foreach (var item in feasibleTimeBound)
                {
                    double itemM3 = (double)item.Volume; 
                    double itemKg = (double)item.Weight;

                    if (curKg + itemKg <= allowedKg && curM3 + itemM3 <= allowedM3)
                    {
                        curKg += itemKg;
                        curM3 += itemM3;

                        selected.Add(new PreAssignProduct
                        {
                            PostId = item.Post.Id,
                            ProductId = item.Post.ProductId,
                            UserName = item.UserName,
                            Address = item.Address,
                            DimensionText = item.DimensionText,
                            Weight = itemKg,
                            Volume = Math.Round(itemM3, 5)
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
                        TotalVolume = Math.Round(selected.Sum(x => x.Volume), 5),
                        SuggestedVehicle = new SuggestedVehicle
                        {
                            Id = suggested.VehicleId,
                            Plate_Number = suggested.Plate_Number,
                            Vehicle_Type = suggested.Vehicle_Type,
                            Capacity_Kg = suggested.Capacity_Kg,
                            AllowedCapacityKg = Math.Round(allowedKg, 2),
                            Capacity_M3 = Math.Round((double)suggested.Capacity_M3, 4),
                            AllowedCapacityM3 = Math.Round(allowedM3, 4)
                        },
                        Products = selected
                    });
                }

                if (!pool.Any()) break;
            }

            return await Task.FromResult(res);
        }

        public async Task<bool> AssignDayAsync(AssignDayRequest request)
        {
            var vehicle = FakeDataSeeder.vehicles.FirstOrDefault(v => v.VehicleId == request.VehicleId)
                ?? throw new Exception("Xe không tồn tại.");

            if (vehicle.Small_Collection_Point != request.CollectionPointId)
            {
                throw new Exception($"Xe {vehicle.Plate_Number} không thuộc trạm này.");
            }

            if (!FakeDataSeeder.smallCollectionPoints.Any(p => p.SmallCollectionPointsId == request.CollectionPointId))
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
            var point = FakeDataSeeder.smallCollectionPoints.FirstOrDefault(p => p.SmallCollectionPointsId == request.CollectionPointId)
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
                var posts = FakeDataSeeder.posts.Where(p => assignDay.ProductIds.Contains(p.ProductId)).ToList();
                if (!posts.Any()) continue;

                var shifts = FakeDataSeeder.shifts
                    .Where(s => s.WorkDate == workDate && s.Vehicle_Id == assignDay.VehicleId)
                    .OrderBy(s => s.Shift_Start_Time).ToList();

                if (!shifts.Any()) throw new Exception($"Ngày {workDate} xe {assignDay.VehicleId} chưa có lịch làm việc.");

                var vehicle = FakeDataSeeder.vehicles.First(v => v.VehicleId == assignDay.VehicleId);
                var mainShift = shifts.First();
                mainShift.Status = "Scheduled";

                var oldRoutes = FakeDataSeeder.collectionRoutes
                    .Where(r => r.CollectionDate == workDate && assignDay.ProductIds.Contains(r.ProductId)).ToList();
                var oldGroupIds = oldRoutes.Select(r => r.CollectionGroupId).Distinct().ToList();
                foreach (var gid in oldGroupIds)
                {
                    FakeDataSeeder.collectionRoutes.RemoveAll(r => r.CollectionGroupId == gid);
                    FakeDataSeeder.collectionGroups.RemoveAll(g => g.CollectionGroupId == gid);
                }

                var locations = new List<(double lat, double lng)>();
                var nodesToOptimize = new List<OptimizationNode>();
                var mapData = new List<dynamic>();

                locations.Add((point.Latitude, point.Longitude));

                foreach (var p in posts)
                {
                    var user = FakeDataSeeder.users.First(u => u.UserId == p.SenderId);
                    var address = _userAddress.FirstOrDefault(a => a.UserId == p.SenderId);

                    if (address == null || !TryGetTimeWindowForDate(p.ScheduleJson!, workDate, out var st, out var en))
                        continue;

                    var product = FakeDataSeeder.products.First(pr => pr.ProductId == p.ProductId);
                    var cat = FakeDataSeeder.categories.FirstOrDefault(c => c.CategoryId == product.CategoryId);
                    var brand = FakeDataSeeder.brands.FirstOrDefault(b => b.BrandId == product.BrandId);
                    var att = GetProductAttributes(p.ProductId);
                    locations.Add((address.Iat ?? point.Latitude, address.Ing ?? point.Longitude));

                    nodesToOptimize.Add(new OptimizationNode
                    {
                        OriginalIndex = mapData.Count,
                        Weight = att.weight,
                        Volume = att.volume, 
                        Start = st,
                        End = en
                    });

                    mapData.Add(new
                    {
                        Post = p,
                        User = user,
                        Address = address,
                        CategoryName = cat?.Name ?? "Unknown",
                        BrandName = brand?.Name ?? "Unknown",
                        Att = new
                        {
                            Length = att.length,
                            Width = att.width,
                            Height = att.height,
                            Weight = att.weight,
                            Volume = att.volume, 
                            DimensionText = att.dimensionText
                        }
                    });
                }

                if (!nodesToOptimize.Any()) continue;

                var (matrixDist, matrixTime) = await _matrixClient.GetMatrixAsync(locations);

                var sortedIndices = RouteOptimizer.SolveVRP(
                    matrixDist,
                    matrixTime,
                    nodesToOptimize,
                    vehicle.Capacity_Kg,
                    vehicle.Capacity_M3,
                    TimeOnly.FromDateTime(mainShift.Shift_Start_Time),
                    TimeOnly.FromDateTime(mainShift.Shift_End_Time)
                );

                if (!sortedIndices.Any())
                {
                    Console.WriteLine("CẢNH BÁO: Không tìm thấy lộ trình tối ưu. Đang dùng lộ trình mặc định (Fallback).");
                    sortedIndices = Enumerable.Range(0, nodesToOptimize.Count).ToList();
                }

                int newGroupId = FakeDataSeeder.collectionGroups.Count + 1;
                var group = new CollectionGroups
                {
                    CollectionGroupId = newGroupId,
                    Group_Code = $"GRP-{workDate:MMdd}-{groupCounter++}",
                    Shift_Id = mainShift.ShiftId,
                    Name = $"{vehicle.Vehicle_Type} - {vehicle.Plate_Number}",
                    Created_At = DateTime.Now
                };

                var routeNodes = new List<RouteDetail>();
                var saveRoutes = new List<CollectionRoutes>();

                TimeOnly cursorTime = TimeOnly.FromDateTime(mainShift.Shift_Start_Time);
                int prevLocIdx = 0;
                double totalKg = 0;
                double totalM3 = 0;

                for (int i = 0; i < sortedIndices.Count; i++)
                {
                    int originalIdx = sortedIndices[i];
                    int currentLocIdx = originalIdx + 1;

                    var data = mapData[originalIdx];
                    var node = nodesToOptimize[originalIdx];

                    long distMeters = matrixDist[prevLocIdx, currentLocIdx];
                    long timeSec = matrixTime[prevLocIdx, currentLocIdx];

                    var arrival = cursorTime.AddMinutes(timeSec / 60.0);
                    if (arrival < node.Start) arrival = node.Start;

                    routeNodes.Add(new RouteDetail
                    {
                        PickupOrder = i + 1,
                        ProductId = data.Post.ProductId,
                        UserName = data.User.Name,
                        Address = data.Address.Address,
                        DistanceKm = Math.Round(distMeters / 1000.0, 2),
                        EstimatedArrival = arrival.ToString("HH:mm"),
                        Schedule = JsonSerializer.Deserialize<object>(data.Post.ScheduleJson!),
                        CategoryName = data.CategoryName,
                        BrandName = data.BrandName,
                        DimensionText = data.Att.DimensionText,
                        WeightKg = data.Att.Weight,
                        VolumeM3 = data.Att.Volume 
                    });

                    saveRoutes.Add(new CollectionRoutes
                    {
                        CollectionRouteId = Guid.NewGuid(),
                        CollectionGroupId = group.CollectionGroupId,
                        ProductId = data.Post.ProductId,
                        CollectionDate = workDate,
                        EstimatedTime = arrival,
                        DistanceKm = Math.Round(distMeters / 1000.0, 2),
                        Status = "Chưa bắt đầu"
                    });

                    cursorTime = arrival.AddMinutes(15);
                    prevLocIdx = currentLocIdx;
                    totalKg += node.Weight;
                    totalM3 += node.Volume;
                }

                if (request.SaveResult)
                {
                    FakeDataSeeder.collectionGroups.Add(group);
                    FakeDataSeeder.collectionRoutes.AddRange(saveRoutes);
                }
                var collectorObj = FakeDataSeeder.users.FirstOrDefault(u => u.UserId == mainShift.CollectorId);
                string collectorName = collectorObj != null ? collectorObj.Name : "Chưa chỉ định";

                response.CreatedGroups.Add(new GroupSummary
                {
                    GroupId = group.CollectionGroupId,
                    GroupCode = group.Group_Code,
                    Collector = collectorName,
                    Vehicle = $"{vehicle.Plate_Number} ({vehicle.Vehicle_Type})",
                    ShiftId = mainShift.ShiftId,
                    GroupDate = workDate,
                    TotalPosts = routeNodes.Count,
                    TotalWeightKg = Math.Round(totalKg, 2),
                    TotalVolumeM3 = Math.Round(totalM3, 3),
                    Routes = routeNodes
                });
            }

            return await Task.FromResult(response);
        }

        public async Task<object> GetRoutesByGroupAsync(int groupId)
        {
            var group = FakeDataSeeder.collectionGroups
                .FirstOrDefault(g => g.CollectionGroupId == groupId)
                ?? throw new Exception("Không tìm thấy group.");

            var shift = FakeDataSeeder.shifts.First(s => s.ShiftId == group.Shift_Id);


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
                .First(p => p.SmallCollectionPointsId == staging.PointId);

            var vehicle = FakeDataSeeder.vehicles
                .First(v => v.VehicleId == staging.VehicleId);

            var collector = FakeDataSeeder.users
                .FirstOrDefault(c => c.UserId == shift.CollectorId);

            double totalWeight = 0, totalVolume = 0;

            int order = 1;
            var routeList = new List<object>();

            foreach (var r in routes)
            {
                var post = FakeDataSeeder.posts.FirstOrDefault(p => p.ProductId == r.ProductId)
                    ?? throw new Exception("Không tìm thấy Post cho Product");
                var user = FakeDataSeeder.users.First(u => u.UserId == post.SenderId);
                var userAddress = _userAddress.First(a => a.UserId == user.UserId);
                var product = FakeDataSeeder.products.First(pr => pr.ProductId == r.ProductId);
                var category = FakeDataSeeder.categories.FirstOrDefault(c => c.CategoryId == product.CategoryId);
                var brand = FakeDataSeeder.brands.FirstOrDefault(b => b.BrandId == product.BrandId);

                var att = GetProductAttributes(post.ProductId);

                totalWeight += att.weight;
                totalVolume += att.volume;

                routeList.Add(new
                {
                    pickupOrder = order++,
                    productId = post.ProductId,
                    postId = post.PostId,
                    userName = user.Name,
                    address = userAddress.Address,
                    categoryName = category?.Name ?? "Unknown",
                    brandName = brand?.Name ?? "Unknown",
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
                groupId = group.CollectionGroupId,
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
        public async Task<List<object>> GetGroupsByPointIdAsync(string pointId)
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
                var group = FakeDataSeeder.collectionGroups.First(g => g.CollectionGroupId == gid);
                var shift = FakeDataSeeder.shifts.First(s => s.ShiftId == group.Shift_Id);

                var routes = FakeDataSeeder.collectionRoutes
                    .Where(r => r.CollectionGroupId == gid)
                    .ToList();

                if (!routes.Any()) continue;

                var firstProductId = routes.First().ProductId;

                var staging = FakeDataSeeder.stagingAssignDays
                    .First(s => s.Date == shift.WorkDate &&
                                s.ProductIds.Contains(firstProductId));

                var vehicle = FakeDataSeeder.vehicles.First(v => v.VehicleId == staging.VehicleId);

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
                    groupId = group.CollectionGroupId,
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
                    .OrderBy(v => v.VehicleId)
                    .ToList()
            );
        }

        public async Task<List<Vehicles>> GetVehiclesBySmallPointAsync(string smallPointId)
        {
            var vehicles = FakeDataSeeder.vehicles
                .Where(v => v.Status == "active" && v.Small_Collection_Point == smallPointId)
                .OrderBy(v => v.VehicleId)
                .ToList();

            return await Task.FromResult(vehicles);
        }

        public async Task<List<PendingPostModel>> GetPendingPostsAsync()
        {
            var posts = FakeDataSeeder.posts
                .Where(p =>
                    FakeDataSeeder.products.Any(pr =>
                        pr.ProductId == p.ProductId &&
                        pr.Status == "Chờ gom nhóm"))
                .ToList();

            var result = new List<PendingPostModel>();

            foreach (var p in posts)
            {
                var user = FakeDataSeeder.users.First(u => u.UserId == p.SenderId);
                var product = FakeDataSeeder.products.First(pr => pr.ProductId == p.ProductId);
                var address = _userAddress.First(a => a.UserId == user.UserId);

                var brand = FakeDataSeeder.brands.First(b => b.BrandId == product.BrandId);
                var cat = FakeDataSeeder.categories.First(c => c.CategoryId == product.CategoryId);

                var att = GetProductAttributes(p.ProductId);

                result.Add(new PendingPostModel
                {
                    PostId = p.PostId,
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
        public async Task<ReassignGroupResponse> ReassignGroupAsync(ReassignGroupRequest request)
        {
            var group = FakeDataSeeder.collectionGroups.FirstOrDefault(g => g.CollectionGroupId == request.GroupId)
                ?? throw new Exception("Không tìm thấy nhóm thu gom.");

            var oldShift = FakeDataSeeder.shifts.FirstOrDefault(s => s.ShiftId == group.Shift_Id)
                ?? throw new Exception("Không tìm thấy ca làm việc gắn với group này.");

            var workDate = oldShift.WorkDate;
            var vehicleId = oldShift.Vehicle_Id; 

            var vehicleObj = FakeDataSeeder.vehicles.FirstOrDefault(v => v.VehicleId == vehicleId)
                ?? throw new Exception("Không tìm thấy thông tin xe.");
            var currentPointId = vehicleObj.Small_Collection_Point;

            var newCollector = FakeDataSeeder.users.FirstOrDefault(u => u.UserId == request.NewCollectorId)
                ?? throw new Exception("Nhân viên mới không tồn tại.");

            var otherShifts = FakeDataSeeder.shifts
                .Where(s => s.WorkDate == workDate && s.CollectorId == request.NewCollectorId)
                .ToList();

            foreach (var shift in otherShifts)
            {
                var v = FakeDataSeeder.vehicles.FirstOrDefault(x => x.VehicleId == shift.Vehicle_Id);
                if (v != null && v.Small_Collection_Point != currentPointId)
                {
                    throw new Exception($"Nhân viên {newCollector.Name} đang có lịch làm việc tại trạm khác (Trạm ID: {v.Small_Collection_Point}) vào ngày này.");
                }
            }
            var existingShiftOnThisVehicle = otherShifts.FirstOrDefault(s => s.Vehicle_Id == vehicleId);

            var targetShiftId = "";

            if (existingShiftOnThisVehicle != null)
            {

                targetShiftId = existingShiftOnThisVehicle.ShiftId;
            }
            else
            {


                int newShiftId = FakeDataSeeder.shifts.Any() ? FakeDataSeeder.shifts.Max(s => int.Parse(s.ShiftId)) + 1 : 1;

                var newShift = new Shifts
                {
                    ShiftId = newShiftId.ToString(),
                    CollectorId = request.NewCollectorId,
                    Vehicle_Id = vehicleId,              
                    WorkDate = workDate,                  
                    Shift_Start_Time = oldShift.Shift_Start_Time, 
                    Shift_End_Time = oldShift.Shift_End_Time,
                    Status = "Replacement"                
                };

                FakeDataSeeder.shifts.Add(newShift);
                targetShiftId = newShiftId.ToString();
            }

            group.Shift_Id = targetShiftId;

            group.Name = $"{vehicleObj.Vehicle_Type} - {vehicleObj.Plate_Number} ({newCollector.Name})";

            return new ReassignGroupResponse
            {
                Success = true,
                Message = "Thay thế nhân viên thành công.",
                GroupId = group.CollectionGroupId,
                CollectorName = newCollector.Name
            };
        }
    }
}
