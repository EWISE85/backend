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

        // Lưu trữ tạm thời trong RAM 
        private static readonly List<StagingAssignDayModel> _inMemoryStaging = new();

        private readonly IUnitOfWork _unitOfWork;
        private readonly MapboxMatrixClient _matrixClient;

        public GroupingService(IUnitOfWork unitOfWork, MapboxMatrixClient matrixClient)
        {
            _unitOfWork = unitOfWork;
            _matrixClient = matrixClient;
        }

        public async Task<PreAssignResponse> PreAssignAsync(PreAssignRequest request)
        {
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(request.CollectionPointId)
                ?? throw new Exception($"Không tìm thấy trạm thu gom (ID: {request.CollectionPointId})");

            var vehicles = await _unitOfWork.Vehicles.GetAllAsync(v =>
                v.Small_Collection_Point == request.CollectionPointId &&
                v.Status == VehicleStatus.DANG_HOAT_DONG.ToString());

            var pointVehicles = vehicles.OrderBy(v => v.Capacity_Kg).ToList();
            if (!pointVehicles.Any()) throw new Exception($"Trạm '{point.Name}' hiện không có xe nào đang hoạt động.");

            var rawPosts = await _unitOfWork.Posts.GetAllAsync(
                filter: p => p.AssignedSmallPointId == request.CollectionPointId,
                includeProperties: "Product"
            );

            if (!rawPosts.Any()) throw new Exception($"Trạm '{point.Name}' hiện chưa được gán đơn hàng nào.");

            string targetStatus = ProductStatus.CHO_GOM_NHOM.ToString();
            var statusPosts = rawPosts.Where(p =>
                p.Product != null &&
                string.Equals(p.Product.Status?.Trim(), targetStatus, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            if (!statusPosts.Any()) throw new Exception($"Không có đơn nào ở trạng thái '{targetStatus}' tại trạm này.");

            var validPosts = statusPosts;
            if (request.ProductIds != null && request.ProductIds.Any())
            {
                validPosts = statusPosts.Where(p => request.ProductIds.Contains(p.ProductId)).ToList();
                if (!validPosts.Any()) throw new Exception("Các ProductId gửi lên không tồn tại trong danh sách chờ gom.");
            }

            var pool = new List<dynamic>();

            var attIdMap = await GetAttributeIdMapAsync();

            foreach (var p in validPosts)
            {
                if (TryParseScheduleInfo(p.ScheduleJson!, out var sch))
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(p.SenderId);

                    var cat = p.Product.Category;
                    if (cat == null) cat = await _unitOfWork.Categories.GetByIdAsync(p.Product.CategoryId);

                    var metrics = await GetProductMetricsInternalAsync(p.ProductId, attIdMap);

                    string dimensionStr = $"{metrics.length} x {metrics.width} x {metrics.height}";

                    pool.Add(new
                    {
                        Post = p,
                        Schedule = sch,
                        Weight = metrics.weight,
                        Volume = metrics.volume,
                        Length = metrics.length,
                        Width = metrics.width,
                        Height = metrics.height,
                        DimensionText = dimensionStr,

                        UserName = user?.Name ?? "Khách vãng lai",
                        Address = !string.IsNullOrEmpty(p.Address) ? p.Address : "Chưa cập nhật",
                        CategoryName = cat?.Name ?? "Khác"
                    });
                }
            }

            if (!pool.Any()) throw new Exception("Không đọc được lịch trình của các đơn hàng tìm thấy.");

            var distinctDates = pool.SelectMany(x => (List<DateOnly>)x.Schedule.SpecificDates)
                                    .Distinct().OrderBy(x => x).ToList();

            var res = new PreAssignResponse
            {
                CollectionPoint = point.Name,
                LoadThresholdPercent = request.LoadThresholdPercent,
                Days = new List<PreAssignDay>()
            };

            // DUYỆT TỪNG NGÀY
            foreach (var date in distinctDates)
            {
                // Lấy tất cả đơn hàng của ngày đó
                var candidates = pool
                    .Where(x => ((List<DateOnly>)x.Schedule.SpecificDates).Contains(date))
                    .OrderBy(x => x.Schedule.MaxDate)
                    .ToList();

                if (!candidates.Any()) continue;

                // Copy danh sách xe để tính toán (có thể dùng lại xe nếu cho phép chạy nhiều chuyến)
                // Ở đây giả sử mỗi xe chạy 1 chuyến, nếu hết xe thì tái sử dụng xe to nhất
                var availableVehicles = pointVehicles.OrderBy(v => v.Capacity_Kg).ToList();

                // VÒNG LẶP: CHIA ĐƠN CHO ĐẾN KHI HẾT HÀNG
                while (candidates.Any())
                {
                    double totalWeightNeed = candidates.Sum(x => (double)x.Weight);
                    double totalVolumeNeed = candidates.Sum(x => (double)x.Volume);

                    // 1. CHỌN XE PHÙ HỢP NHẤT CHO SỐ HÀNG CÒN LẠI
                    Vehicles suggested = null;
                    double selectedVehicleVol = 0;

                    // Tìm xe vừa đủ tải (Smallest Fit)
                    foreach (var v in availableVehicles)
                    {
                        double vVol = v.Length_M * v.Width_M * v.Height_M;
                        double safeKg = v.Capacity_Kg * (request.LoadThresholdPercent / 100.0);
                        double safeVol = vVol * (request.LoadThresholdPercent / 100.0);

                        // Nếu xe này chở hết được đống hàng còn lại -> Chọn luôn
                        if (totalWeightNeed <= safeKg && totalVolumeNeed <= safeVol)
                        {
                            suggested = v;
                            selectedVehicleVol = vVol;
                            break;
                        }
                    }

                    // Nếu không có xe nào chở hết được 1 lần (hàng quá nhiều) -> Chọn xe to nhất hiện có để chở bớt
                    if (suggested == null)
                    {
                        suggested = availableVehicles.Last(); // Xe to nhất
                        selectedVehicleVol = suggested.Length_M * suggested.Width_M * suggested.Height_M;
                    }

                    // 2. TÍNH TOÁN NGƯỠNG CHO XE ĐÃ CHỌN
                    double ratio = request.LoadThresholdPercent / 100.0;
                    double allowedKg = suggested.Capacity_Kg * ratio;
                    double allowedM3 = selectedVehicleVol * ratio;

                    double curKg = 0;
                    double curM3 = 0;

                    var selectedProducts = new List<PreAssignProduct>();
                    var removeList = new List<dynamic>();

                    // 3. XẾP HÀNG VÀO XE (Bin Packing - First Fit)
                    foreach (var item in candidates)
                    {
                        double itemKg = (double)item.Weight;
                        double itemM3 = (double)item.Volume;

                        // Nếu còn nhét vừa thì nhét vào
                        if ((curKg + itemKg <= allowedKg) && (curM3 + itemM3 <= allowedM3))
                        {
                            curKg += itemKg;
                            curM3 += itemM3;

                            selectedProducts.Add(new PreAssignProduct
                            {
                                PostId = item.Post.PostId,
                                ProductId = item.Post.ProductId,
                                UserName = item.UserName,
                                Address = item.Address,
                                Weight = itemKg,
                                Volume = Math.Round(itemM3, 5),
                                Length = item.Length,
                                Width = item.Width,
                                Height = item.Height,
                                DimensionText = item.DimensionText
                            });

                            removeList.Add(item);
                        }
                    }

                    // 4. TẠO KẾT QUẢ CHO CHUYẾN XE NÀY
                    if (selectedProducts.Any())
                    {
                        res.Days.Add(new PreAssignDay
                        {
                            WorkDate = date,
                            OriginalPostCount = selectedProducts.Count,
                            TotalWeight = Math.Round(selectedProducts.Sum(x => x.Weight), 2),
                            TotalVolume = Math.Round(selectedProducts.Sum(x => x.Volume), 5),
                            SuggestedVehicle = new SuggestedVehicle
                            {
                                Id = suggested.VehicleId.ToString(),
                                Plate_Number = suggested.Plate_Number,
                                Vehicle_Type = suggested.Vehicle_Type,
                                Capacity_Kg = suggested.Capacity_Kg,
                                AllowedCapacityKg = Math.Round(allowedKg, 2),
                                Capacity_M3 = Math.Round(selectedVehicleVol, 4),
                                AllowedCapacityM3 = Math.Round(allowedM3, 4)
                            },
                            Products = selectedProducts
                        });

                        // Xóa các đơn đã được xếp khỏi danh sách chờ
                        foreach (var x in removeList) candidates.Remove(x);
                        foreach (var x in removeList) pool.Remove(x); // Xóa khỏi pool tổng để không lặp lại ở ngày sau (nếu logic cần)

                        // Nếu muốn logic: "Mỗi xe chỉ chạy 1 chuyến/ngày", thì bỏ xe này ra khỏi availableVehicles
                        // availableVehicles.Remove(suggested); 
                        // if (!availableVehicles.Any()) break; // Hết xe thì dừng, phần còn lại (nếu có) sẽ bị sót
                    }
                    else
                    {
                        // Trường hợp đơn hàng quá to (1 đơn > sức chứa xe), tránh vòng lặp vô tận
                        break;
                    }
                }
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

            var busyOtherPoint = _inMemoryStaging.Any(s =>
           s.Date == request.WorkDate &&
           s.VehicleId == request.VehicleId &&
           s.PointId != request.CollectionPointId);

            if (busyOtherPoint)
                throw new Exception($"Xe đã được điều động nơi khác vào ngày này.");

            _inMemoryStaging.RemoveAll(s =>
                    s.Date == request.WorkDate &&
                    s.PointId == request.CollectionPointId &&
                    s.VehicleId == request.VehicleId);

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
        //Không sử dụng mapbox
        public async Task<GroupingByPointResponse> GroupByCollectionPointAsync(GroupingByPointRequest request)
        {
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(request.CollectionPointId)
                ?? throw new Exception("Không tìm thấy trạm.");

            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();
            double serviceTime = GetConfigValue(allConfigs, null, point.SmallCollectionPointsId, SystemConfigKey.SERVICE_TIME_MINUTES, 15); 

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
            var attMap = await GetAttributeIdMapAsync();

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
                            s.Status == ShiftStatus.CO_SAN.ToString() &&
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
                        selectedShift.Status = ShiftStatus.DA_LEN_LICH.ToString();
                        selectedShift.WorkDate = workDate;
                        mainShift = selectedShift;
                        _unitOfWork.Shifts.Update(mainShift);
                    }
                    else
                    {
                        throw new Exception($"Ngày {workDate}: Xe {assignDay.VehicleId} cần hoạt động nhưng không tìm thấy tài xế (Shift) nào rảnh.");
                    }
                }

                if (mainShift.Status == ShiftStatus.CO_SAN.ToString())
                {
                    mainShift.Status = ShiftStatus.DA_LEN_LICH.ToString();
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

                // Điểm đầu tiên là Kho
                locations.Add((point.Latitude, point.Longitude));

                var shiftStart = TimeOnly.FromDateTime(mainShift.Shift_Start_Time.AddHours(7));
                var shiftEnd = TimeOnly.FromDateTime(mainShift.Shift_End_Time.AddHours(7));

                foreach (var p in posts)
                {
                    double lat = point.Latitude;
                    double lng = point.Longitude;
                    string displayAddress = p.Address ?? "Không có địa chỉ";

                    if (!string.IsNullOrEmpty(p.Address))
                    {
                        var matchedAddress = await _unitOfWork.UserAddresses.GetAsync(a => a.UserId == p.SenderId && a.Address == p.Address);
                        if (matchedAddress != null && matchedAddress.Iat.HasValue && matchedAddress.Ing.HasValue)
                        {
                            if (Math.Abs(matchedAddress.Iat.Value) > 0.0001 && Math.Abs(matchedAddress.Ing.Value) > 0.0001)
                            {
                                lat = matchedAddress.Iat.Value;
                                lng = matchedAddress.Ing.Value;
                            }
                            else
                            {
                                displayAddress += " (Lỗi tọa độ 0,0 - Đã gán về Trạm)";
                            }
                        }
                        else
                        {
                            displayAddress += " (Không tìm thấy tọa độ - Đã gán về Trạm)";
                        }
                    }
                    else
                    {
                        displayAddress += " (Địa chỉ trống - Đã gán về Trạm)";
                    }

                    TimeOnly finalStart = shiftStart;
                    TimeOnly finalEnd = shiftEnd;

                    if (TryGetTimeWindowForDate(p.ScheduleJson!, workDate, out var st, out var en))
                    {
                        var clampedStart = st < shiftStart ? shiftStart : st;
                        var clampedEnd = en > shiftEnd ? shiftEnd : en;
                        if (clampedStart < clampedEnd)
                        {
                            finalStart = clampedStart;
                            finalEnd = clampedEnd;
                        }
                    }

                    var metrics = await GetProductMetricsInternalAsync(p.ProductId, attMap);
                    string dimStr = $"{metrics.length} x {metrics.width} x {metrics.height}";

                    locations.Add((lat, lng));

                    nodesToOptimize.Add(new OptimizationNode
                    {
                        OriginalIndex = mapData.Count,
                        Weight = metrics.weight,
                        Volume = metrics.volume,
                        Start = finalStart,
                        End = finalEnd
                    });

                    var user = await _unitOfWork.Users.GetByIdAsync(p.SenderId);
                    var product = await _unitOfWork.Products.GetByIdAsync(p.ProductId);
                    var cat = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId);
                    var brand = await _unitOfWork.Brands.GetByIdAsync(product.BrandId);

                    mapData.Add(new
                    {
                        Post = p,
                        User = user,
                        DisplayAddress = displayAddress,
                        CategoryName = cat?.Name ?? "Không rõ",
                        BrandName = brand?.Name ?? "Không rõ",
                        Att = new
                        {
                            Length = metrics.length,
                            Width = metrics.width,
                            Height = metrics.height,
                            Weight = metrics.weight,
                            Volume = metrics.volume,
                            DimensionText = dimStr
                        }
                    });
                }

                if (!nodesToOptimize.Any()) continue;

                // --- TÍNH MA TRẬN KHOẢNG CÁCH (KHÔNG DÙNG MAPBOX) ---

                // ma trận
                int locCount = locations.Count;
                long[,] matrixDist = new long[locCount, locCount];
                long[,] matrixTime = new long[locCount, locCount];

                // Fix cứng tạm nhé !!
                double speedMetersPerSec = 30.0 * 1000 / 3600; 
                double tortuosity = 1.4; // Hệ số đường vòng

                //Tính toán 
                for (int i = 0; i < locCount; i++)
                {
                    for (int j = 0; j < locCount; j++)
                    {
                        if (i == j)
                        {
                            matrixDist[i, j] = 0;
                            matrixTime[i, j] = 0;
                        }
                        else
                        {
                            double distKm = GeoHelper.DistanceKm(
                                locations[i].lat, locations[i].lng,
                                locations[j].lat, locations[j].lng
                            );

                            long distMeters = (long)(distKm * 1000 * tortuosity);

                            matrixDist[i, j] = distMeters;
                            matrixTime[i, j] = (long)(distMeters / speedMetersPerSec);
                        }
                    }
                }

                double calculatedVehicleVolume = vehicle.Length_M * vehicle.Width_M * vehicle.Height_M;

                // GỌI THUẬT TOÁN OR-TOOLS
                var sortedIndices = RouteOptimizer.SolveVRP(
                    matrixDist, matrixTime, nodesToOptimize,
                    vehicle.Capacity_Kg, calculatedVehicleVolume,
                    shiftStart, shiftEnd
                );

                if (!sortedIndices.Any()) sortedIndices = Enumerable.Range(0, nodesToOptimize.Count).ToList();

                // --- TẠO GROUP & ROUTE VÀO DB ---
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
                TimeOnly cursorTime = shiftStart;
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
                        productToUpdate.Status = ProductStatus.CHO_THU_GOM.ToString();
                        _unitOfWork.Products.Update(productToUpdate);
                        if (request.SaveResult)
                        {
                            await _unitOfWork.ProductStatusHistory.AddAsync(new ProductStatusHistory
                            {
                                ProductStatusHistoryId = Guid.NewGuid(),
                                ProductId = productToUpdate.ProductId,
                                ChangedAt = DateTime.UtcNow,
                                Status = ProductStatus.CHO_THU_GOM.ToString(),
                                StatusDescription = $"Đơn hàng đã được xếp lịch cho xe {vehicle.Plate_Number}."
                            });
                        }
                    }

                    long distMeters = matrixDist[prevLocIdx, currentLocIdx];
                    long timeSec = matrixTime[prevLocIdx, currentLocIdx];

                    var arrival = cursorTime.AddMinutes(timeSec / 60.0);
                    var node = nodesToOptimize[originalIdx];
                    if (arrival < node.Start) arrival = node.Start;

                    routeNodes.Add(new RouteDetail
                    {
                        PickupOrder = i + 1,
                        ProductId = data.Post.ProductId,
                        UserName = data.User.Name,
                        Address = data.DisplayAddress,
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
                            Status = CollectionRouteStatus.CHUA_BAT_DAU.ToString(),
                            ConfirmImages = new List<string>()
                        });
                    }

                    cursorTime = arrival.AddMinutes(serviceTime);
                    prevLocIdx = currentLocIdx;
                    totalKg += (double)data.Att.Weight;
                    totalM3 += (double)data.Att.Volume;
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

        //Sử dụng mapbox 
        //public async Task<GroupingByPointResponse> GroupByCollectionPointAsync(GroupingByPointRequest request)
        //{
        //    var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(request.CollectionPointId)
        //        ?? throw new Exception("Không tìm thấy trạm.");

        //    var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();
        //    double serviceTime = GetConfigValue(allConfigs, null, point.SmallCollectionPointsId, SystemConfigKey.SERVICE_TIME_MINUTES, DEFAULT_SERVICE_TIME);

        //    var staging = _inMemoryStaging
        //        .Where(s => s.PointId == request.CollectionPointId)
        //        .OrderBy(s => s.Date)
        //        .ToList();

        //    if (!staging.Any()) throw new Exception("Chưa có dữ liệu Assign. Hãy chạy AssignDay trước.");

        //    var response = new GroupingByPointResponse
        //    {
        //        CollectionPoint = point.Name,
        //        SavedToDatabase = request.SaveResult
        //    };

        //    int groupCounter = 1;

        //    var attMap = await GetAttributeIdMapAsync();

        //    foreach (var assignDay in staging)
        //    {
        //        var workDate = assignDay.Date;
        //        var posts = new List<Post>();
        //        foreach (var pid in assignDay.ProductIds)
        //        {
        //            var p = await _unitOfWork.Posts.GetAsync(x => x.ProductId == pid);
        //            if (p != null) posts.Add(p);
        //        }

        //        if (!posts.Any()) continue;

        //        var assignedShift = await _unitOfWork.Shifts.GetAsync(s => s.WorkDate == workDate && s.Vehicle_Id == assignDay.VehicleId);
        //        Shifts mainShift;

        //        if (assignedShift != null)
        //        {
        //            mainShift = assignedShift;
        //        }
        //        else
        //        {
        //            var availableShifts = await _unitOfWork.Shifts.GetAllAsync(s =>
        //                    s.WorkDate == workDate &&
        //                    s.Status == ShiftStatus.CO_SAN.ToString() &&
        //                    string.IsNullOrEmpty(s.Vehicle_Id)
        //            );

        //            var shiftList = availableShifts.ToList();
        //            Shifts? selectedShift = null;

        //            foreach (var sh in shiftList)
        //            {
        //                var collector = await _unitOfWork.Users.GetByIdAsync(sh.CollectorId);
        //                if (collector != null && collector.SmallCollectionPointId == request.CollectionPointId)
        //                {
        //                    selectedShift = sh;
        //                    break;
        //                }
        //            }

        //            if (selectedShift != null)
        //            {
        //                selectedShift.Vehicle_Id = assignDay.VehicleId;
        //                selectedShift.Status = ShiftStatus.DA_LEN_LICH.ToString();
        //                selectedShift.WorkDate = workDate;
        //                mainShift = selectedShift;
        //                _unitOfWork.Shifts.Update(mainShift);
        //            }
        //            else
        //            {

        //                throw new Exception($"Ngày {workDate}: Xe {assignDay.VehicleId} cần hoạt động nhưng không tìm thấy tài xế nào rảnh.");
        //            }
        //        }

        //        if (mainShift.Status == ShiftStatus.CO_SAN.ToString())
        //        {
        //            mainShift.Status = ShiftStatus.DA_LEN_LICH.ToString();
        //            _unitOfWork.Shifts.Update(mainShift);
        //        }

        //        var oldGroups = await _unitOfWork.CollectionGroups.GetAllAsync(g => g.Shift_Id == mainShift.ShiftId);
        //        foreach (var g in oldGroups)
        //        {
        //            var routes = await _unitOfWork.CollecctionRoutes.GetAllAsync(r => r.CollectionGroupId == g.CollectionGroupId);
        //            foreach (var r in routes) _unitOfWork.CollecctionRoutes.Delete(r);
        //            _unitOfWork.CollectionGroups.Delete(g);
        //        }

        //        var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(assignDay.VehicleId);
        //        var locations = new List<(double lat, double lng)>();
        //        var nodesToOptimize = new List<OptimizationNode>();
        //        var mapData = new List<dynamic>();

        //        locations.Add((point.Latitude, point.Longitude));

        //        var shiftStart = TimeOnly.FromDateTime(mainShift.Shift_Start_Time.AddHours(7));
        //        var shiftEnd = TimeOnly.FromDateTime(mainShift.Shift_End_Time.AddHours(7));

        //        foreach (var p in posts)
        //        {
        //            double lat = point.Latitude;
        //            double lng = point.Longitude;
        //            bool isAddressValid = false;
        //            string displayAddress = p.Address ?? "Không có địa chỉ";

        //            if (!string.IsNullOrEmpty(p.Address))
        //            {
        //                var matchedAddress = await _unitOfWork.UserAddresses.GetAsync(a => a.UserId == p.SenderId && a.Address == p.Address);
        //                if (matchedAddress != null && matchedAddress.Iat.HasValue && matchedAddress.Ing.HasValue)
        //                {
        //                    lat = matchedAddress.Iat.Value;
        //                    lng = matchedAddress.Ing.Value;
        //                    isAddressValid = true;
        //                }
        //                else
        //                {
        //                    displayAddress += " (Lỗi tọa độ - Đã gán về Trạm)";
        //                }
        //            }
        //            else
        //            {
        //                displayAddress += " (Trống - Đã gán về Trạm)";
        //            }

        //            TimeOnly finalStart = shiftStart;
        //            TimeOnly finalEnd = shiftEnd;

        //            if (TryGetTimeWindowForDate(p.ScheduleJson!, workDate, out var st, out var en))
        //            {
        //                var clampedStart = st < shiftStart ? shiftStart : st;
        //                var clampedEnd = en > shiftEnd ? shiftEnd : en;

        //                if (clampedStart < clampedEnd)
        //                {
        //                    finalStart = clampedStart;
        //                    finalEnd = clampedEnd;
        //                }
        //                // Nếu giờ khách chọn nằm ngoài ca hoàn toàn -> giữ nguyên shiftStart/End để ép gom
        //            }

        //            var metrics = await GetProductMetricsInternalAsync(p.ProductId, attMap);
        //            string dimStr = $"{metrics.length} x {metrics.width} x {metrics.height}";

        //            locations.Add((lat, lng));

        //            nodesToOptimize.Add(new OptimizationNode
        //            {
        //                OriginalIndex = mapData.Count,
        //                Weight = metrics.weight,
        //                Volume = metrics.volume,
        //                Start = finalStart,
        //                End = finalEnd
        //            });

        //            var user = await _unitOfWork.Users.GetByIdAsync(p.SenderId);
        //            var product = await _unitOfWork.Products.GetByIdAsync(p.ProductId);
        //            var cat = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId);
        //            var brand = await _unitOfWork.Brands.GetByIdAsync(product.BrandId);

        //            mapData.Add(new
        //            {
        //                Post = p,
        //                User = user,
        //                DisplayAddress = displayAddress,
        //                CategoryName = cat?.Name ?? "Không rõ",
        //                BrandName = brand?.Name ?? "Không rõ",
        //                Att = new
        //                {
        //                    Length = metrics.length,
        //                    Width = metrics.width,
        //                    Height = metrics.height,
        //                    Weight = metrics.weight,
        //                    Volume = metrics.volume,
        //                    DimensionText = dimStr
        //                }
        //            });
        //        }

        //        if (!nodesToOptimize.Any()) continue;

        //        // --- GỌI GOOGLE OR-TOOLS (VRP) ---
        //        var (matrixDist, matrixTime) = await _matrixClient.GetMatrixAsync(locations);
        //        double calculatedVehicleVolume = vehicle.Length_M * vehicle.Width_M * vehicle.Height_M;

        //        // Lưu ý: Hàm SolveVRP đã được sửa để trả về Full List (kể cả node bị drop)
        //        var sortedIndices = RouteOptimizer.SolveVRP(
        //            matrixDist, matrixTime, nodesToOptimize,
        //            vehicle.Capacity_Kg, calculatedVehicleVolume,
        //            shiftStart, shiftEnd
        //        );

        //        // Fallback an toàn: Nếu Solver trả về rỗng (hiếm), dùng thứ tự gốc
        //        if (!sortedIndices.Any()) sortedIndices = Enumerable.Range(0, nodesToOptimize.Count).ToList();

        //        // --- TẠO GROUP & ROUTE ---
        //        var group = new CollectionGroups
        //        {
        //            Group_Code = $"GRP-{workDate:MMdd}-{groupCounter++}",
        //            Shift_Id = mainShift.ShiftId,
        //            Name = $"{vehicle.Vehicle_Type} - {vehicle.Plate_Number}",
        //            Created_At = DateTime.UtcNow.AddHours(7)
        //        };

        //        if (request.SaveResult)
        //        {
        //            await _unitOfWork.CollectionGroups.AddAsync(group);
        //            await _unitOfWork.SaveAsync();
        //        }

        //        var routeNodes = new List<RouteDetail>();
        //        TimeOnly cursorTime = shiftStart;
        //        int prevLocIdx = 0;
        //        double totalKg = 0;
        //        double totalM3 = 0;

        //        for (int i = 0; i < sortedIndices.Count; i++)
        //        {
        //            int originalIdx = sortedIndices[i];
        //            int currentLocIdx = originalIdx + 1;
        //            var data = mapData[originalIdx];

        //            var productToUpdate = await _unitOfWork.Products.GetByIdAsync((Guid)data.Post.ProductId);
        //            if (productToUpdate != null)
        //            {
        //                productToUpdate.Status = ProductStatus.CHO_THU_GOM.ToString();
        //                _unitOfWork.Products.Update(productToUpdate);
        //                if (request.SaveResult)
        //                {
        //                    await _unitOfWork.ProductStatusHistory.AddAsync(new ProductStatusHistory
        //                    {
        //                        ProductStatusHistoryId = Guid.NewGuid(),
        //                        ProductId = productToUpdate.ProductId,
        //                        ChangedAt = DateTime.UtcNow,
        //                        Status = ProductStatus.CHO_THU_GOM.ToString(),
        //                        StatusDescription = $"Đơn hàng đã được xếp lịch cho xe {vehicle.Plate_Number}."
        //                    });
        //                }
        //            }

        //            // Tính toán thời gian đến dự kiến
        //            var node = nodesToOptimize[originalIdx];
        //            long distMeters = matrixDist[prevLocIdx, currentLocIdx];
        //            long timeSec = matrixTime[prevLocIdx, currentLocIdx];

        //            var arrival = cursorTime.AddMinutes(timeSec / 60.0);
        //            // Nếu đến sớm hơn giờ mở cửa của khách -> Chờ
        //            if (arrival < node.Start) arrival = node.Start;

        //            // Tạo Route Detail
        //            routeNodes.Add(new RouteDetail
        //            {
        //                PickupOrder = i + 1,
        //                ProductId = data.Post.ProductId,
        //                UserName = data.User.Name,
        //                Address = data.DisplayAddress,
        //                DistanceKm = Math.Round(distMeters / 1000.0, 2),
        //                EstimatedArrival = arrival.ToString("HH:mm"),
        //                Schedule = JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>((string)data.Post.ScheduleJson,
        //                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
        //                CategoryName = data.CategoryName,
        //                BrandName = data.BrandName,
        //                DimensionText = data.Att.DimensionText,
        //                WeightKg = data.Att.Weight,
        //                VolumeM3 = data.Att.Volume
        //            });

        //            if (request.SaveResult)
        //            {
        //                await _unitOfWork.CollecctionRoutes.AddAsync(new CollectionRoutes
        //                {
        //                    CollectionRouteId = Guid.NewGuid(),
        //                    CollectionGroupId = group.CollectionGroupId,
        //                    ProductId = data.Post.ProductId,
        //                    CollectionDate = workDate,
        //                    EstimatedTime = arrival,
        //                    DistanceKm = Math.Round(distMeters / 1000.0, 2),
        //                    Status = CollectionRouteStatus.CHUA_BAT_DAU.ToString(),
        //                    ConfirmImages = new List<string>()
        //                });
        //            }

        //            cursorTime = arrival.AddMinutes(serviceTime);
        //            prevLocIdx = currentLocIdx;

        //            totalKg += (double)data.Att.Weight;
        //            totalM3 += (double)data.Att.Volume;
        //        }

        //        if (request.SaveResult) await _unitOfWork.SaveAsync();

        //        var collectorObj = await _unitOfWork.Users.GetByIdAsync(mainShift.CollectorId);
        //        string collectorName = collectorObj != null ? collectorObj.Name : "Chưa chỉ định";

        //        response.CreatedGroups.Add(new GroupSummary
        //        {
        //            GroupId = group.CollectionGroupId,
        //            GroupCode = group.Group_Code,
        //            Collector = collectorName,
        //            Vehicle = $"{vehicle.Plate_Number} ({vehicle.Vehicle_Type})",
        //            ShiftId = mainShift.ShiftId,
        //            GroupDate = workDate,
        //            TotalPosts = routeNodes.Count,
        //            TotalWeightKg = Math.Round(totalKg, 2),
        //            TotalVolumeM3 = Math.Round(totalM3, 3),
        //            Routes = routeNodes
        //        });
        //    }

        //    return response;
        //}

        public async Task<List<object>> GetGroupsByPointIdAsync(string collectionPointId)
        {
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(collectionPointId);
            if (point == null) throw new Exception("Trạm thu gom không tồn tại.");

            var allGroups = await _unitOfWork.CollectionGroups.GetAllAsync();
            var result = new List<object>();

            var attMap = await GetAttributeIdMapAsync();

            foreach (var group in allGroups)
            {
                var shift = await _unitOfWork.Shifts.GetByIdAsync(group.Shift_Id);
                if (shift == null) continue;

                bool isMatch = false;
                string vehicleInfo = "Không rõ";
                string collectorInfo = "Không rõ";

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
                        var metrics = await GetProductMetricsInternalAsync(r.ProductId, attMap);
                        totalW += metrics.weight;
                        totalV += metrics.volume;
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

            var attMap = await GetAttributeIdMapAsync();

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
                var metrics = await GetProductMetricsInternalAsync(post.ProductId, attMap);
                string dimStr = $"{metrics.length} x {metrics.width} x {metrics.height}";

                totalWeight += metrics.weight;
                totalVolume += metrics.volume;

                routeList.Add(new
                {
                    pickupOrder = order++,
                    productId = post.ProductId,
                    postId = post.PostId,
                    userName = user.Name,
                    address = post.Address ?? "Không có",
                    categoryName = category?.Name ?? "Không rõ",
                    brandName = brand?.Name ?? "Không rõ",
                    dimensionText = dimStr,
                    weightKg = metrics.weight,
                    volumeM3 = metrics.volume,

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
                vehicle = vehicle != null ? $"{vehicle.Plate_Number} ({vehicle.Vehicle_Type})" : "Không rõ",
                collector = collector?.Name ?? "Không rõ",
                groupDate = shift.WorkDate.ToString("yyyy-MM-dd"),
                collectionPoint = point?.Name ?? "Không rõ",
                totalPosts = sortedRoutes.Count,
                totalWeightKg = Math.Round(totalWeight, 2),
                totalVolumeM3 = Math.Round(totalVolume, 2),
                routes = routeList
            };
        }

        public async Task<List<Vehicles>> GetVehiclesAsync()
        {
            var list = await _unitOfWork.Vehicles.GetAllAsync(v => v.Status == VehicleStatus.DANG_HOAT_DONG.ToString());
            return list.OrderBy(v => v.VehicleId).ToList();
        }

        public async Task<List<Vehicles>> GetVehiclesBySmallPointAsync(string smallPointId)
        {
            var list = await _unitOfWork.Vehicles.GetAllAsync(v =>
            v.Status == VehicleStatus.DANG_HOAT_DONG.ToString()
            && v.Small_Collection_Point == smallPointId);
            return list.OrderBy(v => v.VehicleId).ToList();
        }

        public async Task<List<PendingPostModel>> GetPendingPostsAsync()
        {
            var posts = await _unitOfWork.Posts.GetAllAsync(includeProperties: "Product");
            var pendingPosts = posts.Where(p => p.Product != null && p.Product.Status == ProductStatus.CHO_GOM_NHOM.ToString()).ToList();
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
                    Address = !string.IsNullOrEmpty(p.Address) ? p.Address : "Không có",
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

            if (request.ServiceTimeMinutes.HasValue)
            {
                await UpsertConfigAsync(null, point.SmallCollectionPointsId, SystemConfigKey.SERVICE_TIME_MINUTES, request.ServiceTimeMinutes.Value.ToString());
            }

            if (request.AvgTravelTimeMinutes.HasValue)
            {
                await UpsertConfigAsync(null, point.SmallCollectionPointsId, SystemConfigKey.AVG_TRAVEL_TIME_MINUTES, request.AvgTravelTimeMinutes.Value.ToString());
            }

            await _unitOfWork.SaveAsync();
            return true;
        }

        public async Task<CompanySettingsResponse> GetCompanySettingsAsync(string companyId)
        {
            var company = await _unitOfWork.Companies.GetByIdAsync(companyId);
            if (company == null) throw new Exception($"Không tìm thấy công ty với ID: {companyId}");
            var points = await _unitOfWork.SmallCollectionPoints.GetAllAsync(p => p.CompanyId == companyId);

            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();

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
                    ServiceTimeMinutes = GetConfigValue(allConfigs, null, p.SmallCollectionPointsId, SystemConfigKey.SERVICE_TIME_MINUTES, DEFAULT_SERVICE_TIME),
                    AvgTravelTimeMinutes = GetConfigValue(allConfigs, null, p.SmallCollectionPointsId, SystemConfigKey.AVG_TRAVEL_TIME_MINUTES, DEFAULT_TRAVEL_TIME),
                    IsDefault = false
                });
            }
            return response;
        }

        public async Task<SinglePointSettingResponse> GetPointSettingAsync(string pointId)
        {
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(pointId);
            if (point == null) throw new Exception("Trạm thu gom không tồn tại.");
            var company = await _unitOfWork.Companies.GetByIdAsync(point.CompanyId);

            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();

            return new SinglePointSettingResponse
            {
                CompanyId = company?.CompanyId ?? "Không rõ",
                CompanyName = company?.Name ?? "Không rõ",
                SmallPointId = point.SmallCollectionPointsId,
                SmallPointName = point.Name,
                ServiceTimeMinutes = GetConfigValue(allConfigs, null, point.SmallCollectionPointsId, SystemConfigKey.SERVICE_TIME_MINUTES, DEFAULT_SERVICE_TIME),
                AvgTravelTimeMinutes = GetConfigValue(allConfigs, null, point.SmallCollectionPointsId, SystemConfigKey.AVG_TRAVEL_TIME_MINUTES, DEFAULT_TRAVEL_TIME),
                IsDefault = false
            };
        }
        private async Task<Dictionary<string, Guid>> GetAttributeIdMapAsync()
        {
            var targetKeywords = new[] { "Trọng lượng", "Khối lượng giặt", "Chiều dài", "Chiều rộng", "Chiều cao", "Dung tích", "Kích thước màn hình" };
            var allAttributes = await _unitOfWork.Attributes.GetAllAsync();
            var map = new Dictionary<string, Guid>();

            foreach (var key in targetKeywords)
            {
                var match = allAttributes.FirstOrDefault(a => a.Name.Contains(key, StringComparison.OrdinalIgnoreCase));
                if (match != null && !map.ContainsKey(key)) map.Add(key, match.AttributeId);
            }
            return map;
        }

        private async Task<(double weight, double volume, double length, double width, double height)> GetProductMetricsInternalAsync(Guid productId, Dictionary<string, Guid> attMap)
        {
            var pValues = await _unitOfWork.ProductValues.GetAllAsync(filter: v => v.ProductId == productId);
            var optionIds = pValues.Where(v => v.AttributeOptionId.HasValue).Select(v => v.AttributeOptionId.Value).ToList();

            var relatedOptions = optionIds.Any()
                ? (await _unitOfWork.AttributeOptions.GetAllAsync(filter: o => optionIds.Contains(o.OptionId))).ToList()
                : new List<AttributeOptions>();

            double weight = 0;
            var weightKeys = new[] { "Trọng lượng", "Khối lượng giặt", "Dung tích" };
            foreach (var key in weightKeys)
            {
                if (!attMap.ContainsKey(key)) continue;
                var pVal = pValues.FirstOrDefault(v => v.AttributeId == attMap[key]);
                if (pVal != null)
                {
                    if (pVal.AttributeOptionId.HasValue)
                    {
                        var opt = relatedOptions.FirstOrDefault(o => o.OptionId == pVal.AttributeOptionId);
                        if (opt != null && opt.EstimateWeight.HasValue && opt.EstimateWeight > 0) { weight = opt.EstimateWeight.Value; break; }
                    }
                    if (pVal.Value.HasValue && pVal.Value.Value > 0) { weight = pVal.Value.Value; break; }
                }
            }
            if (weight <= 0) weight = 3;

            double GetVal(string k)
            {
                if (!attMap.ContainsKey(k)) return 0;
                var pv = pValues.FirstOrDefault(v => v.AttributeId == attMap[k]);
                return pv?.Value ?? 0;
            }
            double length = GetVal("Chiều dài");
            double width = GetVal("Chiều rộng");
            double height = GetVal("Chiều cao");

            double volume = 0;
            if (length > 0 && width > 0 && height > 0)
            {
                volume = length * width * height;
            }
            else
            {
                var volKeys = new[] { "Kích thước màn hình", "Dung tích", "Khối lượng giặt", "Trọng lượng" };
                foreach (var key in volKeys)
                {
                    if (!attMap.ContainsKey(key)) continue;
                    var pVal = pValues.FirstOrDefault(v => v.AttributeId == attMap[key]);
                    if (pVal != null && pVal.AttributeOptionId.HasValue)
                    {
                        var opt = relatedOptions.FirstOrDefault(o => o.OptionId == pVal.AttributeOptionId);
                        if (opt != null && opt.EstimateVolume.HasValue && opt.EstimateVolume > 0)
                        {
                            volume = opt.EstimateVolume.Value * 1_000_000;
                            break;
                        }
                    }
                }
            }
            if (volume <= 0) volume = 1000;

            return (weight, volume / 1_000_000.0, length, width, height);
        }
        private bool IsUprightRequired(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return false;
            var lowerName = categoryName.ToLower();

            string[] keywords = { "Tủ lạnh", "Lò vi sóng", "Màn hình máy tính", "Máy giặt", "Bình nước nóng", "Tivi" };

            return keywords.Any(k => lowerName.Contains(k));
        }
        private bool IsItemFitInVehicle(double vL, double vW, double vH, double iL, double iW, double iH, bool mustStandUp)
        {
            if (iL <= 0 || iW <= 0 || iH <= 0) return true;

            if (mustStandUp)
            {
                // Bắt buộc: Chiều cao của hàng <= Chiều cao của xe
                if (iH > vH) return false;

                bool fitBaseNormal = (iL <= vL && iW <= vW);
                bool fitBaseRotated = (iL <= vW && iW <= vL);

                return fitBaseNormal || fitBaseRotated;
            }
            else
            {
                // --- LOGIC CHO HÀNG THƯỜNG ---
                // Cho phép xoay 3 chiều thoải mái để nhét vừa
                // Sắp xếp kích thước Xe từ Lớn -> Nhỏ
                var vDims = new[] { vL, vW, vH }.OrderByDescending(x => x).ToArray();

                // Sắp xếp kích thước Hàng từ Lớn -> Nhỏ
                var iDims = new[] { iL, iW, iH }.OrderByDescending(x => x).ToArray();

                return iDims[0] <= vDims[0] &&
                       iDims[1] <= vDims[1] &&
                       iDims[2] <= vDims[2];
            }
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

            if (weight <= 0) weight = 3;
            if (volume <= 0)
            {
                volume = 0.1;
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

        private double GetConfigValue(IEnumerable<SystemConfig> configs, string? companyId, string? pointId, SystemConfigKey key, double defaultValue)
        {
            var config = configs.FirstOrDefault(x =>
                x.Key == key.ToString() &&
                x.SmallCollectionPointId == pointId &&
                pointId != null);

            if (config == null && companyId != null)
            {
                config = configs.FirstOrDefault(x =>
                x.Key == key.ToString() &&
                x.CompanyId == companyId &&
                x.SmallCollectionPointId == null);
            }

            if (config == null)
            {
                config = configs.FirstOrDefault(x =>
               x.Key == key.ToString() &&
               x.CompanyId == null &&
               x.SmallCollectionPointId == null);
            }

            if (config != null && double.TryParse(config.Value, out double result))
            {
                return result;
            }
            return defaultValue;
        }

        private async Task UpsertConfigAsync(string? companyId, string? pointId, SystemConfigKey key, string value)
        {
            var existingConfig = await _unitOfWork.SystemConfig.GetAsync(x =>
                x.Key == key.ToString() &&
                x.CompanyId == companyId &&
                x.SmallCollectionPointId == pointId);

            if (existingConfig != null)
            {
                existingConfig.Value = value;
                _unitOfWork.SystemConfig.Update(existingConfig);
            }
            else
            {
                var newConfig = new SystemConfig
                {
                    SystemConfigId = Guid.NewGuid(),
                    Key = key.ToString(),
                    Value = value,
                    CompanyId = companyId,
                    SmallCollectionPointId = pointId,
                    Status = SystemConfigStatus.DANG_HOAT_DONG.ToString(),
                    DisplayName = key.ToString(),
                    GroupName = pointId != null ? "PointConfig" : "CompanyConfig"
                };
                await _unitOfWork.SystemConfig.AddAsync(newConfig);
            }
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

    }
}