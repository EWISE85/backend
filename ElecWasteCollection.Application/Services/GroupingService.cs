using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Application.Interfaces;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.GroupModel;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System.Text.Json;

namespace ElecWasteCollection.Application.Services
{
    public class GroupingService : IGroupingService
    {
        private const double DEFAULT_SERVICE_TIME = 15;
        private const double DEFAULT_TRAVEL_TIME = 15;

        private static readonly List<StagingAssignDayModel> _inMemoryStaging = new();

        private readonly IUnitOfWork _unitOfWork;
        private readonly MapboxMatrixClient _matrixClient;

        public GroupingService(IUnitOfWork unitOfWork, MapboxMatrixClient matrixClient)
        {
            _unitOfWork = unitOfWork;
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
        private class StagingAssignDayModel
        {
            public Guid StagingId { get; set; } = Guid.NewGuid();
            public DateOnly Date { get; set; }
            public string PointId { get; set; } = null!;
            public string VehicleId { get; set; } = null!;
            public List<Guid> ProductIds { get; set; } = new();
        }

        private async Task<(double length, double width, double height, double weight, double volume, string dimensionText)> GetProductAttributesAsync(Guid productId)
        {
            var pValues = await _unitOfWork.ProductValues.GetAllAsync(v => v.ProductId == productId);
            var pValuesList = pValues.ToList();

            double weight = 0;
            double volume = 0;
            double length = 0;
            double width = 0;
            double height = 0;
            string dimText = "";

            foreach (var val in pValuesList)
            {
                if (val.AttributeOptionId.HasValue)
                {
                    var option = await _unitOfWork.AttributeOptions.GetByIdAsync(val.AttributeOptionId.Value);
                    if (option != null)
                    {
                        if (option.EstimateWeight.HasValue && option.EstimateWeight.Value > 0)
                        {
                            weight = option.EstimateWeight.Value;
                            if (string.IsNullOrEmpty(dimText)) dimText = option.OptionName;
                        }

                        if (option.EstimateVolume.HasValue && option.EstimateVolume.Value > 0)
                        {
                            volume = option.EstimateVolume.Value;
                            dimText = option.OptionName;
                        }
                    }
                }
                else if (val.Value.HasValue && val.Value.Value > 0)
                {
                    var attribute = await _unitOfWork.Attributes.GetByIdAsync(val.AttributeId);
                    if (attribute != null)
                    {
                        string nameLower = attribute.Name.ToLower();
                        if (nameLower.Contains("dài")) length = val.Value.Value;
                        else if (nameLower.Contains("rộng")) width = val.Value.Value;
                        else if (nameLower.Contains("cao")) height = val.Value.Value;
                    }
                }
            }

            if (length > 0 && width > 0 && height > 0)
            {
                volume = (length * width * height) / 1_000_000.0; 
                dimText = $"{length} x {width} x {height} cm";
            }

            if (weight <= 0) weight = 1;
            if (volume <= 0)
            {
                volume = 0.001;
                if (string.IsNullOrEmpty(dimText)) dimText = "Không xác định";
            }
            else if (string.IsNullOrEmpty(dimText))
            {
                dimText = $"~ {Math.Round(volume, 3)} m3";
            }

            return (length, width, height, weight, volume, dimText);
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

                var valid = new List<DateOnly>();
                foreach (var d in days)
                {
                    if (DateOnly.TryParse(d.PickUpDate, out var date) && d.Slots != null &&
                        TimeOnly.TryParse(d.Slots.StartTime, out var s) && TimeOnly.TryParse(d.Slots.EndTime, out var e) && s < e)
                    {
                        valid.Add(date);
                    }
                }
                if (!valid.Any()) return false;
                valid.Sort();
                info.SpecificDates = valid;
                info.MinDate = valid.First();
                info.MaxDate = valid.Last();
                return true;
            }
            catch { return false; }
        }

        private static bool TryGetTimeWindowForDate(string raw, DateOnly target, out TimeOnly start, out TimeOnly end)
        {
            start = TimeOnly.MinValue;
            end = TimeOnly.MaxValue;
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var days = JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>(raw, opts);
                var match = days?.FirstOrDefault(d => DateOnly.TryParse(d.PickUpDate, out var dt) && dt == target);
                if (match?.Slots != null && TimeOnly.TryParse(match.Slots.StartTime, out var s) && TimeOnly.TryParse(match.Slots.EndTime, out var e))
                {
                    start = s;
                    end = e;
                    return true;
                }
                return false;
            }
            catch { return false; }
        }
        public async Task<PreAssignResponse> PreAssignAsync(PreAssignRequest request)
        {
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(request.CollectionPointId)
                ?? throw new Exception("Không tìm thấy trạm thu gom.");

            double serviceTime = point.ServiceTimeMinutes > 0 ? point.ServiceTimeMinutes : DEFAULT_SERVICE_TIME;
            double avgTravelTime = point.AvgTravelTimeMinutes > 0 ? point.AvgTravelTimeMinutes : DEFAULT_TRAVEL_TIME;

            var vehicles = await _unitOfWork.Vehicles.GetAllAsync(v => v.Small_Collection_Point == request.CollectionPointId && v.Status == "active");
            var pointVehicles = vehicles.ToList();

            if (!pointVehicles.Any()) throw new Exception("Trạm này hiện không có xe nào hoạt động.");

            var rawPosts = await _unitOfWork.Posts.GetAllAsync(
                filter: p => p.AssignedSmallPointId == request.CollectionPointId,
                includeProperties: "Product"
            );

            var validPosts = rawPosts.Where(p => p.Product != null && p.Product.Status == "Chờ gom nhóm").ToList();

            if (!validPosts.Any()) throw new Exception("Không có bài đăng nào phù hợp trong hệ thống.");

            var pool = new List<dynamic>();
            foreach (var p in validPosts)
            {
                if (TryParseScheduleInfo(p.ScheduleJson!, out var sch))
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(p.SenderId);
                    var att = await GetProductAttributesAsync(p.ProductId);

                    string postAddress = !string.IsNullOrEmpty(p.Address) ? p.Address : "Chưa cập nhật";

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
                        Address = postAddress
                    });
                }
            }

            if (!pool.Any()) throw new Exception("Không tìm thấy đơn hàng nào có lịch trình hợp lệ.");

            var distinctDates = pool.SelectMany(x => (List<DateOnly>)x.Schedule.SpecificDates)
                                    .Distinct().OrderBy(x => x).ToList();

            var res = new PreAssignResponse
            {
                CollectionPoint = point.Name,
                LoadThresholdPercent = request.LoadThresholdPercent
            };

            foreach (var date in distinctDates)
            {
                var availableVehicles = pointVehicles.OrderBy(v => v.Capacity_Kg).ToList();
                double defaultWorkHours = 8.0; 
                double totalWorkMinutes = availableVehicles.Count * defaultWorkHours * 60;
                double estimatedMinutesPerPost = serviceTime + avgTravelTime;
                int maxPostsByTime = (int)(totalWorkMinutes / estimatedMinutesPerPost);

                var candidates = pool
                    .Where(x => ((List<DateOnly>)x.Schedule.SpecificDates).Contains(date))
                    .OrderBy(x => x.Schedule.MaxDate)
                    .ThenBy(x => x.Schedule.MinDate)
                    .ToList();

                if (!candidates.Any()) continue;

                var feasibleTimeBound = candidates.Take(maxPostsByTime).ToList();

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
                            PostId = item.Post.PostId,
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

            return res;
        }

        public async Task<bool> AssignDayAsync(AssignDayRequest request)
        {
            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(request.VehicleId)
                ?? throw new Exception("Xe không tồn tại.");

            if (vehicle.Small_Collection_Point != request.CollectionPointId)
                throw new Exception($"Xe {vehicle.Plate_Number} không thuộc trạm này.");

            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(request.CollectionPointId);
            if (point == null) throw new Exception("Trạm không tồn tại.");

            if (request.ProductIds == null || !request.ProductIds.Any())
                throw new Exception("Danh sách Product trống.");

            var busyList = _inMemoryStaging.Where(s =>
                s.Date == request.WorkDate &&
                s.VehicleId == request.VehicleId &&
                s.PointId != request.CollectionPointId).ToList();

            if (busyList.Any())
                throw new Exception($"Xe {vehicle.Plate_Number} đã được điều động nơi khác vào ngày này.");

            _inMemoryStaging.RemoveAll(s => s.Date == request.WorkDate && s.PointId == request.CollectionPointId);

            _inMemoryStaging.Add(new StagingAssignDayModel
            {
                StagingId = Guid.NewGuid(),
                Date = request.WorkDate,
                PointId = request.CollectionPointId,
                VehicleId = request.VehicleId,
                ProductIds = request.ProductIds
            });

            return await Task.FromResult(true);
        }

        public async Task<GroupingByPointResponse> GroupByCollectionPointAsync(GroupingByPointRequest request)
        {
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(request.CollectionPointId)
                ?? throw new Exception("Không tìm thấy trạm.");

            double serviceTime = point.ServiceTimeMinutes > 0 ? point.ServiceTimeMinutes : DEFAULT_SERVICE_TIME;

            var staging = _inMemoryStaging
                .Where(s => s.PointId == request.CollectionPointId)
                .OrderBy(s => s.Date)
                .ToList();

            if (!staging.Any()) throw new Exception("Chưa có dữ liệu Assign. Hãy chạy AssignDay trước.");

            var response = new GroupingByPointResponse
            {
                CollectionPoint = point.Name,
                SavedToDatabase = request.SaveResult
            };

            int groupCounter = 1;

            foreach (var assignDay in staging)
            {
                var workDate = assignDay.Date;
                var posts = new List<Post>();
                foreach (var pid in assignDay.ProductIds)
                {
                    var p = await _unitOfWork.Posts.GetAsync(x => x.ProductId == pid);
                    if (p != null) posts.Add(p);
                }

                if (!posts.Any()) continue;

                var assignedShift = await _unitOfWork.Shifts.GetAsync(s => s.WorkDate == workDate && s.Vehicle_Id == assignDay.VehicleId);
                Shifts mainShift;

                if (assignedShift != null)
                {
                    mainShift = assignedShift;
                }
                else
                {
                    var availableShifts = await _unitOfWork.Shifts.GetAllAsync(s =>
                          s.WorkDate == workDate &&
                          s.Status == "Available" &&
                          string.IsNullOrEmpty(s.Vehicle_Id)
                    );

                    var shiftList = availableShifts.ToList();
                    Shifts? selectedShift = null;

                    foreach (var sh in shiftList)
                    {
                        var collector = await _unitOfWork.Users.GetByIdAsync(sh.CollectorId);
                        if (collector != null && collector.SmallCollectionPointId == request.CollectionPointId)
                        {
                            selectedShift = sh;
                            break;
                        }
                    }

                    if (selectedShift != null)
                    {
                        selectedShift.Vehicle_Id = assignDay.VehicleId;
                        selectedShift.Status = "Scheduled";
                        selectedShift.WorkDate = workDate;
                        mainShift = selectedShift;
                        _unitOfWork.Shifts.Update(mainShift);
                    }
                    else
                    {
                        throw new Exception($"Ngày {workDate}: Xe {assignDay.VehicleId} cần hoạt động nhưng không tìm thấy tài xế nào rảnh.");
                    }
                }

                if (mainShift.Status == "Available" || mainShift.Status == "Assigned")
                {
                    mainShift.Status = "Scheduled";
                    _unitOfWork.Shifts.Update(mainShift);
                }

                var oldGroups = await _unitOfWork.CollectionGroups.GetAllAsync(g => g.Shift_Id == mainShift.ShiftId);
                foreach (var g in oldGroups)
                {
                    var routes = await _unitOfWork.CollecctionRoutes.GetAllAsync(r => r.CollectionGroupId == g.CollectionGroupId);
                    foreach (var r in routes) _unitOfWork.CollecctionRoutes.Delete(r);
                    _unitOfWork.CollectionGroups.Delete(g);
                }
                var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(assignDay.VehicleId);
                var locations = new List<(double lat, double lng)>();
                var nodesToOptimize = new List<OptimizationNode>();
                var mapData = new List<dynamic>();

                locations.Add((point.Latitude, point.Longitude));

                foreach (var p in posts)
                {
                
                    if (string.IsNullOrEmpty(p.Address)) continue;

                    var matchedAddress = await _unitOfWork.UserAddresses.GetAsync(a =>
                        a.UserId == p.SenderId &&
                        a.Address == p.Address
                    );

                    if (matchedAddress == null || matchedAddress.Iat == null || matchedAddress.Ing == null)
                        continue;

                    if (!TryGetTimeWindowForDate(p.ScheduleJson!, workDate, out var st, out var en))
                        continue;

                    var user = await _unitOfWork.Users.GetByIdAsync(p.SenderId);

                    var shiftStart = TimeOnly.FromDateTime(mainShift.Shift_Start_Time.AddHours(7));
                    var shiftEnd = TimeOnly.FromDateTime(mainShift.Shift_End_Time.AddHours(7));
                    var actualStart = st > shiftStart ? st : shiftStart;
                    var actualEnd = en < shiftEnd ? en : shiftEnd;

                    if (actualStart >= actualEnd) continue;

                    st = actualStart;
                    en = actualEnd;

                    var product = await _unitOfWork.Products.GetByIdAsync(p.ProductId);
                    var cat = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId);
                    var brand = await _unitOfWork.Brands.GetByIdAsync(product.BrandId);
                    var att = await GetProductAttributesAsync(p.ProductId);

                    locations.Add((matchedAddress.Iat.Value, matchedAddress.Ing.Value));

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
                        Address = p.Address, 
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
                    matrixDist, matrixTime, nodesToOptimize,
                    vehicle.Capacity_Kg, vehicle.Capacity_M3,
                     TimeOnly.FromDateTime(mainShift.Shift_Start_Time.AddHours(7)),
                    TimeOnly.FromDateTime(mainShift.Shift_End_Time.AddHours(7))
                );

                if (!sortedIndices.Any()) sortedIndices = Enumerable.Range(0, nodesToOptimize.Count).ToList();

                var group = new CollectionGroups
                {
                    Group_Code = $"GRP-{workDate:MMdd}-{groupCounter++}",
                    Shift_Id = mainShift.ShiftId,
                    Name = $"{vehicle.Vehicle_Type} - {vehicle.Plate_Number}",
                    Created_At = DateTime.UtcNow.AddHours(7)
                };

                if (request.SaveResult)
                {
                    await _unitOfWork.CollectionGroups.AddAsync(group);
                    await _unitOfWork.SaveAsync();
                }

                var routeNodes = new List<RouteDetail>();
                TimeOnly cursorTime = TimeOnly.FromDateTime(mainShift.Shift_Start_Time);
                int prevLocIdx = 0;
                double totalKg = 0;
                double totalM3 = 0;

                for (int i = 0; i < sortedIndices.Count; i++)
                {
                    int originalIdx = sortedIndices[i];
                    int currentLocIdx = originalIdx + 1;
                    var data = mapData[originalIdx];

                    var productToUpdate = await _unitOfWork.Products.GetByIdAsync((Guid)data.Post.ProductId);
                    if (productToUpdate != null)
                    {
                        productToUpdate.Status = "Chờ thu gom";
                        _unitOfWork.Products.Update(productToUpdate);

                        if (request.SaveResult)
                        {
                            await _unitOfWork.ProductStatusHistory.AddAsync(new ProductStatusHistory
                            {
                                ProductStatusHistoryId = Guid.NewGuid(),
                                ProductId = productToUpdate.ProductId,
                                ChangedAt = DateTime.UtcNow,
                                Status = "Chờ thu gom",
                                StatusDescription = $"Đơn hàng đã được xếp lịch cho xe {vehicle.Plate_Number}."
                            });
                        }
                    }

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
                        Address = data.Address, // CẬP NHẬT: Lấy từ data.Address (là Post.Address)
                        DistanceKm = Math.Round(distMeters / 1000.0, 2),
                        EstimatedArrival = arrival.ToString("HH:mm"),
                        Schedule = JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>((string)data.Post.ScheduleJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
                        CategoryName = data.CategoryName,
                        BrandName = data.BrandName,
                        DimensionText = data.Att.DimensionText,
                        WeightKg = data.Att.Weight,
                        VolumeM3 = data.Att.Volume
                    });

                    if (request.SaveResult)
                    {
                        await _unitOfWork.CollecctionRoutes.AddAsync(new CollectionRoutes
                        {
                            CollectionRouteId = Guid.NewGuid(),
                            CollectionGroupId = group.CollectionGroupId,
                            ProductId = data.Post.ProductId,
                            CollectionDate = workDate,
                            EstimatedTime = arrival,
                            DistanceKm = Math.Round(distMeters / 1000.0, 2),
                            Status = "Chưa bắt đầu",
                            ConfirmImages = new List<string>()
                        });
                    }

                    cursorTime = arrival.AddMinutes(serviceTime);
                    prevLocIdx = currentLocIdx;
                    totalKg += node.Weight;
                    totalM3 += node.Volume;
                }

                if (request.SaveResult) await _unitOfWork.SaveAsync();

                var collectorObj = await _unitOfWork.Users.GetByIdAsync(mainShift.CollectorId);
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

            return response;
        }

        public async Task<List<object>> GetGroupsByPointIdAsync(string collectionPointId)
        {
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(collectionPointId);
            if (point == null) throw new Exception("Trạm thu gom không tồn tại.");

            var allGroups = await _unitOfWork.CollectionGroups.GetAllAsync();
            var result = new List<object>();

            foreach (var group in allGroups)
            {
                var shift = await _unitOfWork.Shifts.GetByIdAsync(group.Shift_Id);
                if (shift == null) continue;

                bool isMatch = false;
                string vehicleInfo = "Unknown";
                string collectorInfo = "Unknown";

                if (!string.IsNullOrEmpty(shift.Vehicle_Id))
                {
                    var v = await _unitOfWork.Vehicles.GetByIdAsync(shift.Vehicle_Id);
                    if (v != null)
                    {
                        vehicleInfo = $"{v.Plate_Number} ({v.Vehicle_Type})";
                        if (v.Small_Collection_Point == collectionPointId) isMatch = true;
                    }
                }

                var c = await _unitOfWork.Users.GetByIdAsync(shift.CollectorId);
                if (c != null)
                {
                    collectorInfo = c.Name;
                    if (c.SmallCollectionPointId == collectionPointId) isMatch = true;
                }

                if (isMatch)
                {
                    var routes = await _unitOfWork.CollecctionRoutes.GetAllAsync(r => r.CollectionGroupId == group.CollectionGroupId);

                    double totalW = 0;
                    double totalV = 0;

                    foreach (var r in routes)
                    {
                        var post = await _unitOfWork.Posts.GetAsync(p => p.ProductId == r.ProductId);
                        if (post != null)
                        {
                            var att = await GetProductAttributesAsync(post.ProductId);
                            totalW += att.weight;
                            totalV += att.volume;
                        }
                    }

                    result.Add(new
                    {
                        GroupId = group.CollectionGroupId,
                        GroupCode = group.Group_Code,
                        ShiftId = group.Shift_Id,
                        Vehicle = vehicleInfo,
                        Collector = collectorInfo,
                        Date = shift.WorkDate.ToString("yyyy-MM-dd"),
                        TotalOrders = routes.Count(),
                        TotalWeightKg = Math.Round(totalW, 2),
                        TotalVolumeM3 = Math.Round(totalV, 4),
                        CreatedAt = group.Created_At
                    });
                }
            }

            return result.OrderByDescending(x => ((dynamic)x).CreatedAt).ToList();
        }

        public async Task<object> GetRoutesByGroupAsync(int groupId)
        {
            var group = await _unitOfWork.CollectionGroups.GetByIdAsync(groupId) ?? throw new Exception("Không tìm thấy group.");
            var shift = await _unitOfWork.Shifts.GetByIdAsync(group.Shift_Id);
            var routes = await _unitOfWork.CollecctionRoutes.GetAllAsync(r => r.CollectionGroupId == groupId);
            var sortedRoutes = routes.OrderBy(r => r.EstimatedTime).ToList();

            if (!sortedRoutes.Any()) throw new Exception("Group không có route nào.");

            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(shift.Vehicle_Id);
            var collector = await _unitOfWork.Users.GetByIdAsync(shift.CollectorId);

            string pointId = vehicle?.Small_Collection_Point ?? collector?.SmallCollectionPointId;
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(pointId);

            double totalWeight = 0, totalVolume = 0;
            int order = 1;
            var routeList = new List<object>();

            foreach (var r in sortedRoutes)
            {
                var post = await _unitOfWork.Posts.GetAsync(p => p.ProductId == r.ProductId);
                if (post == null) continue;

                var user = await _unitOfWork.Users.GetByIdAsync(post.SenderId);


                var product = await _unitOfWork.Products.GetByIdAsync(r.ProductId);
                var category = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId);
                var brand = await _unitOfWork.Brands.GetByIdAsync(product.BrandId);
                var att = await GetProductAttributesAsync(post.ProductId);

                totalWeight += att.weight;
                totalVolume += att.volume;

                routeList.Add(new
                {
                    pickupOrder = order++,
                    productId = post.ProductId,
                    postId = post.PostId,
                    userName = user.Name,
                    address = post.Address ?? "N/A", 
                    categoryName = category?.Name ?? "Unknown",
                    brandName = brand?.Name ?? "Unknown",
                    dimensionText = att.dimensionText,
                    weightKg = att.weight,
                    volumeM3 = att.volume,
                    distanceKm = r.DistanceKm,
                    schedule = JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>(post.ScheduleJson!,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
                    estimatedArrival = r.EstimatedTime.ToString("HH:mm")
                });
            }

            return new
            {
                groupId = group.CollectionGroupId,
                groupCode = group.Group_Code,
                shiftId = group.Shift_Id,
                vehicle = vehicle != null ? $"{vehicle.Plate_Number} ({vehicle.Vehicle_Type})" : "Unknown",
                collector = collector?.Name ?? "Unknown",
                groupDate = shift.WorkDate.ToString("yyyy-MM-dd"),
                collectionPoint = point?.Name ?? "Unknown",
                totalPosts = sortedRoutes.Count,
                totalWeightKg = Math.Round(totalWeight, 2),
                totalVolumeM3 = Math.Round(totalVolume, 2),
                routes = routeList
            };
        }

        public async Task<List<Vehicles>> GetVehiclesAsync()
        {
            var list = await _unitOfWork.Vehicles.GetAllAsync(v => v.Status == "active");
            return list.OrderBy(v => v.VehicleId).ToList();
        }

        public async Task<List<Vehicles>> GetVehiclesBySmallPointAsync(string smallPointId)
        {
            var list = await _unitOfWork.Vehicles.GetAllAsync(v => v.Status == "active" && v.Small_Collection_Point == smallPointId);
            return list.OrderBy(v => v.VehicleId).ToList();
        }

        public async Task<List<PendingPostModel>> GetPendingPostsAsync()
        {
            var posts = await _unitOfWork.Posts.GetAllAsync(includeProperties: "Product");
            var pendingPosts = posts.Where(p => p.Product != null && p.Product.Status == "Chờ gom nhóm").ToList();
            var result = new List<PendingPostModel>();

            foreach (var p in pendingPosts)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(p.SenderId);
                var product = p.Product;
                var brand = await _unitOfWork.Brands.GetByIdAsync(product.BrandId);
                var cat = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId);
                var att = await GetProductAttributesAsync(p.ProductId);

                result.Add(new PendingPostModel
                {
                    PostId = p.PostId,
                    ProductId = p.ProductId,
                    UserName = user.Name,
                    Address = !string.IsNullOrEmpty(p.Address) ? p.Address : "N/A", 
                    ProductName = $"{brand?.Name} {cat?.Name}",
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
            return result;
        }

        public async Task<bool> UpdatePointSettingAsync(UpdatePointSettingRequest request)
        {
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(request.PointId);
            if (point == null) throw new Exception("Trạm thu gom không tồn tại.");

            if (request.ServiceTimeMinutes.HasValue) point.ServiceTimeMinutes = request.ServiceTimeMinutes.Value;
            if (request.AvgTravelTimeMinutes.HasValue) point.AvgTravelTimeMinutes = request.AvgTravelTimeMinutes.Value;

            _unitOfWork.SmallCollectionPoints.Update(point);
            await _unitOfWork.SaveAsync();
            return true;
        }

        public async Task<CompanySettingsResponse> GetCompanySettingsAsync(string companyId)
        {
            var company = await _unitOfWork.CollectionCompanies.GetByIdAsync(companyId);
            if (company == null) throw new Exception($"Không tìm thấy công ty với ID: {companyId}");
            var points = await _unitOfWork.SmallCollectionPoints.GetAllAsync(p => p.CompanyId == companyId);

            var response = new CompanySettingsResponse
            {
                CompanyId = company.CompanyId,
                CompanyName = company.Name,
                Points = new List<PointSettingDetailDto>()
            };
            foreach (var p in points)
            {
                response.Points.Add(new PointSettingDetailDto
                {
                    SmallPointId = p.SmallCollectionPointsId,
                    SmallPointName = p.Name,
                    ServiceTimeMinutes = p.ServiceTimeMinutes,
                    AvgTravelTimeMinutes = p.AvgTravelTimeMinutes,
                    IsDefault = false
                });
            }
            return response;
        }

        public async Task<SinglePointSettingResponse> GetPointSettingAsync(string pointId)
        {
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(pointId);
            if (point == null) throw new Exception("Trạm thu gom không tồn tại.");
            var company = await _unitOfWork.CollectionCompanies.GetByIdAsync(point.CompanyId);
            return new SinglePointSettingResponse
            {
                CompanyId = company?.CompanyId ?? "Unknown",
                CompanyName = company?.Name ?? "Unknown Company",
                SmallPointId = point.SmallCollectionPointsId,
                SmallPointName = point.Name,
                ServiceTimeMinutes = point.ServiceTimeMinutes,
                AvgTravelTimeMinutes = point.AvgTravelTimeMinutes,
                IsDefault = false
            };
        }
    }
}