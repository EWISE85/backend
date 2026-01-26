using DocumentFormat.OpenXml.Spreadsheet;
using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Application.Interfaces;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.GroupModel;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ElecWasteCollection.Application.Services
{
    public class GroupingService : IGroupingService
    {

        private const double DEFAULT_SERVICE_TIME = 15;
        private const double DEFAULT_TRAVEL_TIME = 15;

        // Lưu trữ tạm thời trong RAM 
        private static readonly List<StagingAssignDayModel> _inMemoryStaging = new();
        private static readonly object _lockObj = new object();

        private readonly ICollectionGroupRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly MapboxMatrixClient _matrixClient;
        private static readonly List<PreAssignResponse> _previewCache = new();


        public GroupingService(IUnitOfWork unitOfWork, MapboxMatrixClient matrixClient, ICollectionGroupRepository repository)
        {
            _unitOfWork = unitOfWork;
            _matrixClient = matrixClient;
            _repository = repository;

        }

        public async Task<PreAssignResponse> PreAssignAsync(PreAssignRequest request)
        {
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(request.CollectionPointId)
                ?? throw new Exception($"Không tìm thấy trạm (ID: {request.CollectionPointId})");

            var vehicles = await _unitOfWork.Vehicles.GetAllAsync(v =>
                v.Small_Collection_Point == request.CollectionPointId &&
                v.Status == VehicleStatus.DANG_HOAT_DONG.ToString());

            var availableVehicles = vehicles.OrderByDescending(v => v.Capacity_Kg).ToList();
            if (!availableVehicles.Any())
                throw new Exception($"Trạm '{point.Name}' hiện không có xe nào đang hoạt động.");

            var rawPosts = await _unitOfWork.Posts.GetAllAsync(
                filter: p => p.AssignedSmallPointId == request.CollectionPointId,
                includeProperties: "Product"
            );

            string targetStatus = ProductStatus.CHO_GOM_NHOM.ToString();
            var statusPosts = rawPosts.Where(p =>
                p.Product != null &&
                string.Equals(p.Product.Status?.Trim(), targetStatus, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            if (request.ProductIds != null && request.ProductIds.Any())
            {
                statusPosts = statusPosts.Where(p => request.ProductIds.Contains(p.ProductId)).ToList();
            }

            if (!statusPosts.Any())
                throw new Exception("Không có đơn hàng nào hợp lệ.");

            var pool = new List<dynamic>();
            var attIdMap = await GetAttributeIdMapAsync();
            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();

            double serviceTimeMin = GetConfigValue(allConfigs, null, point.SmallCollectionPointsId, SystemConfigKey.SERVICE_TIME_MINUTES, 5);
            double avgTravelTimeMin = GetConfigValue(allConfigs, null, point.SmallCollectionPointsId, SystemConfigKey.AVG_TRAVEL_TIME_MINUTES, 10);

            foreach (var p in statusPosts)
            {
                if (TryParseScheduleInfo(p.ScheduleJson!, out var sch))
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(p.SenderId);
                    var cat = p.Product.Category ?? await _unitOfWork.Categories.GetByIdAsync(p.Product.CategoryId);
                    var metrics = await GetProductMetricsInternalAsync(p.ProductId, attIdMap);

                    pool.Add(new
                    {
                        Post = p,
                        Schedule = sch,
                        Weight = metrics.weight,
                        Volume = metrics.volume,
                        Length = metrics.length,
                        Width = metrics.width,
                        Height = metrics.height,
                        DimensionText = $"{metrics.length} x {metrics.width} x {metrics.height}",
                        UserName = user?.Name ?? "Khách",
                        Address = p.Address,
                        CategoryName = cat?.Name
                    });
                }
            }

            var distinctDates = pool.SelectMany(x => (List<DateOnly>)x.Schedule.SpecificDates)
                .Distinct().OrderBy(x => x).ToList();

            var shifts = await _unitOfWork.Shifts.GetAllAsync(
                filter: s =>
                    distinctDates.Contains(s.WorkDate) &&
                    s.Status == ShiftStatus.CO_SAN.ToString() &&
                    s.Collector.SmallCollectionPointId == request.CollectionPointId, 
                includeProperties: "Collector" 
            );

            var res = new PreAssignResponse
            {
                CollectionPoint = point.Name,
                LoadThresholdPercent = request.LoadThresholdPercent,
                Days = new List<PreAssignDay>()
            };

            foreach (var date in distinctDates)
            {

                var dailyShift = shifts.FirstOrDefault(s => s.WorkDate == date);

                if (dailyShift == null)
                {
                    continue;
                }

                DateTime startDt = dailyShift.Shift_Start_Time.ToLocalTime(); 
                DateTime endDt = dailyShift.Shift_End_Time.ToLocalTime();  
                TimeOnly shiftStartBase = TimeOnly.FromDateTime(startDt);

                double maxShiftMinutes = (endDt - startDt).TotalMinutes;

                var candidatesRaw = pool
                    .Where(x => ((List<DateOnly>)x.Schedule.SpecificDates).Contains(date))
                    .ToList();

                var queue = new List<dynamic>();
                double totalDemandWeight = 0;
                double totalDemandVolume = 0;
                double totalDemandTime = 0;

                foreach (var item in candidatesRaw)
                {
                    TryGetTimeWindowForDate((string)item.Post.ScheduleJson, date, out var s, out var e);

                    if (s < shiftStartBase) s = shiftStartBase;

                    queue.Add(new { Data = item, WindowStart = s, WindowEnd = e });

                    totalDemandWeight += (double)item.Weight;
                    totalDemandVolume += (double)item.Volume;
                    totalDemandTime += (serviceTimeMin + avgTravelTimeMin);
                }

                queue = queue.OrderBy(x => x.WindowEnd).ThenByDescending(x => x.Data.Weight).ToList();

                if (!queue.Any()) continue;

                var vehiclesToUse = new List<Vehicles>();
                double currentCapKg = 0;
                double currentCapM3 = 0;

                foreach (var v in availableVehicles)
                {
                    vehiclesToUse.Add(v); 

                    currentCapKg += v.Capacity_Kg * (request.LoadThresholdPercent / 100.0);
                    currentCapM3 += (v.Length_M * v.Width_M * v.Height_M) * (request.LoadThresholdPercent / 100.0);

                    double estimatedTimePerVehicle = totalDemandTime / vehiclesToUse.Count;

                    bool enoughWeight = currentCapKg >= totalDemandWeight;
                    bool enoughVolume = currentCapM3 >= totalDemandVolume;

                    bool enoughTime = estimatedTimePerVehicle <= maxShiftMinutes;

                    if (enoughWeight && enoughVolume && enoughTime)
                    {
                        break; 
                    }
                }

                var buckets = vehiclesToUse.Select(v => new VehicleBucket
                {
                    Vehicle = v,
                    CurrentTimeMin = 0, 
                    CurrentKg = 0,
                    CurrentM3 = 0,
                    Products = new List<PreAssignProduct>(),
                    MaxKg = v.Capacity_Kg * (request.LoadThresholdPercent / 100.0),
                    MaxM3 = (v.Length_M * v.Width_M * v.Height_M) * (request.LoadThresholdPercent / 100.0)
                }).ToList();

                foreach (var itemWrapper in queue)
                {
                    var item = itemWrapper.Data;
                    double w = (double)item.Weight;
                    double v = (double)item.Volume;

                    var bestBucket = buckets
                        .Where(b => (b.CurrentKg + w <= b.MaxKg) && (b.CurrentM3 + v <= b.MaxM3))
                        .OrderBy(b => b.CurrentTimeMin) 
                        .FirstOrDefault();

                    if (bestBucket == null)
                    {
                        bestBucket = buckets.OrderBy(b => b.CurrentTimeMin).First();
                    }

                    double arrival = bestBucket.CurrentTimeMin + avgTravelTimeMin;

                    double openTime = (itemWrapper.WindowStart - shiftStartBase).TotalMinutes;

                    if (arrival < openTime) arrival = openTime;

                    bestBucket.CurrentTimeMin = arrival + serviceTimeMin;
                    bestBucket.CurrentKg += w;
                    bestBucket.CurrentM3 += v;

                    TimeOnly estArrival = shiftStartBase.AddMinutes(arrival);

                    bestBucket.Products.Add(new PreAssignProduct
                    {
                        PostId = item.Post.PostId,
                        ProductId = item.Post.ProductId,
                        UserName = item.UserName,
                        Address = item.Address,
                        Weight = w,
                        Volume = Math.Round(v, 5),
                        Length = item.Length,
                        Width = item.Width,
                        Height = item.Height,
                        DimensionText = item.DimensionText,
                        //EstimatedArrival = estArrival.ToString("HH:mm") // Giờ đến dự kiến chuẩn theo ca làm việc
                    });
                }

                foreach (var bucket in buckets)
                {
                    if (bucket.Products.Any())
                    {
                        res.Days.Add(new PreAssignDay
                        {
                            WorkDate = date,
                            OriginalPostCount = bucket.Products.Count,
                            TotalWeight = Math.Round(bucket.CurrentKg, 2),
                            TotalVolume = Math.Round(bucket.CurrentM3, 5),
                            SuggestedVehicle = new SuggestedVehicle
                            {
                                Id = bucket.Vehicle.VehicleId.ToString(),
                                Plate_Number = bucket.Vehicle.Plate_Number,
                                Vehicle_Type = bucket.Vehicle.Vehicle_Type,
                                Capacity_Kg = bucket.Vehicle.Capacity_Kg,
                                AllowedCapacityKg = Math.Round(bucket.MaxKg, 2),
                                Capacity_M3 = Math.Round(bucket.Vehicle.Length_M * bucket.Vehicle.Width_M * bucket.Vehicle.Height_M, 4),
                                AllowedCapacityM3 = Math.Round(bucket.MaxM3, 4)
                            },
                            Products = bucket.Products
                        });
                    }
                }
            }

            _previewCache.RemoveAll(x => x.CollectionPoint == point.Name);
            _previewCache.Add(res);

            return res;
        }

        public Task<PreviewProductPagedResult?> GetPreviewProductsAsync(string vehicleId,
            DateOnly workDate, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            foreach (var preview in _previewCache)
            {
                var dayGroup = preview.Days.FirstOrDefault(d =>
                    d.WorkDate == workDate &&
                    d.SuggestedVehicle.Id.Equals(
                        vehicleId,
                        StringComparison.OrdinalIgnoreCase));

                if (dayGroup == null)
                    continue;

                var total = dayGroup.Products.Count;

                var pagedProducts = dayGroup.Products
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Cast<object>() 
                    .ToList();

                var result = new PreviewProductPagedResult
                {
                    VehicleId = dayGroup.SuggestedVehicle.Id,
                    PlateNumber = dayGroup.SuggestedVehicle.Plate_Number,
                    VehicleType = dayGroup.SuggestedVehicle.Vehicle_Type,

                    TotalProduct = total,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize),

                    Products = pagedProducts
                };

                return Task.FromResult<PreviewProductPagedResult?>(result);
            }

            return Task.FromResult<PreviewProductPagedResult?>(null);
        }
        public Task<object> GetPreviewVehiclesAsync(DateOnly workDate)
        {
            var resultList = new List<object>();

            foreach (var preview in _previewCache)
            {
                var vehiclesOnDay = preview.Days
                    .Where(d => d.WorkDate == workDate)
                    .ToList();

                foreach (var group in vehiclesOnDay)
                {
                    resultList.Add(new
                    {
                        VehicleId = group.SuggestedVehicle.Id,
                        PlateNumber = group.SuggestedVehicle.Plate_Number,
                        VehicleType = group.SuggestedVehicle.Vehicle_Type,
                        TotalProduct = group.OriginalPostCount, 
                        TotalWeight = group.TotalWeight,
                        TotalVolume = group.TotalVolume
                    });
                }
            }

            return Task.FromResult<object>(resultList);
        }

        public async Task<bool> AssignDayAsync(AssignDayRequest request)
        {
            if (request.Assignments == null || !request.Assignments.Any())
                throw new Exception("Danh sách điều phối xe trống.");

            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(request.CollectionPointId);
            if (point == null)
                throw new Exception($"Trạm thu gom không tồn tại (ID: {request.CollectionPointId})");

            foreach (var item in request.Assignments)
            {
                if (item.ProductIds == null || !item.ProductIds.Any())
                    throw new Exception($"Xe {item.VehicleId}: Danh sách Product trống.");

                var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(item.VehicleId)
                    ?? throw new Exception($"Xe {item.VehicleId} không tồn tại.");

                if (vehicle.Small_Collection_Point != request.CollectionPointId)
                    throw new Exception($"Xe {vehicle.Plate_Number} không thuộc trạm này.");

                lock (_lockObj)
                {
                    var busyOtherPoint = _inMemoryStaging.Any(s =>
                        s.Date == request.WorkDate &&
                        s.VehicleId == item.VehicleId &&
                        s.PointId != request.CollectionPointId);

                    if (busyOtherPoint)
                        throw new Exception($"Xe {vehicle.Plate_Number} đã được điều động sang trạm khác vào ngày {request.WorkDate}.");
                }
            }

            lock (_lockObj)
            {
                foreach (var item in request.Assignments)
                {
                    // Xóa dữ liệu cũ của chính xe này trong ngày này 
                    _inMemoryStaging.RemoveAll(s =>
                        s.Date == request.WorkDate &&
                        s.VehicleId == item.VehicleId);

                    //Thêm dữ liệu mới
                    _inMemoryStaging.Add(new StagingAssignDayModel
                    {
                        StagingId = Guid.NewGuid(),
                        Date = request.WorkDate,
                        PointId = request.CollectionPointId,
                        VehicleId = item.VehicleId,
                        ProductIds = item.ProductIds
                    });

                    Console.WriteLine($"[RAM] Đã thêm xe {item.VehicleId} vào bộ nhớ.");
                }

                var countXe = _inMemoryStaging.Count(s => s.Date == request.WorkDate && s.PointId == request.CollectionPointId);
                Console.WriteLine($"[RAM CHECK] Hiện tại Staging đang chứa {countXe} xe cho ngày {request.WorkDate}.");
            }

            return await Task.FromResult(true);
        }

        // Hàm Group Không Mapbox
        public async Task<GroupingByPointResponse> GroupByCollectionPointAsync(GroupingByPointRequest request)
        {
            var response = new GroupingByPointResponse
            {
                SavedToDatabase = request.SaveResult,
                CreatedGroups = new List<GroupSummary>(),
                Errors = new List<string>(),
                Logs = new List<string>() 
            };

            try
            {
                var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(request.CollectionPointId);
                if (point == null) throw new Exception($"Không tìm thấy trạm ID: {request.CollectionPointId}");

                response.CollectionPoint = point.Name;
                response.Logs.Add($"[START] Bắt đầu xử lý cho trạm: {point.Name} (ID: {request.CollectionPointId})");

                var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();
                double serviceTime = GetConfigValue(allConfigs, null, point.SmallCollectionPointsId, SystemConfigKey.SERVICE_TIME_MINUTES, 15);

                var staging = _inMemoryStaging
                    .Where(s => s.PointId == request.CollectionPointId)
                    .ToList();

                response.Logs.Add($"[CHECK STAGING] Tìm thấy {staging.Count} dòng dữ liệu phân công (assignments) trong RAM.");

                if (!staging.Any())
                {
                    response.Errors.Add("Bộ nhớ Staging rỗng. Vui lòng chạy lại AssignDay trước khi chạy Group.");
                    return response;
                }

                var distinctDates = staging.Select(s => s.Date).Distinct().ToList();
                var availableShiftQueues = new Dictionary<DateOnly, Queue<Shifts>>();

                foreach (var date in distinctDates)
                {
                    var rawShifts = await _unitOfWork.Shifts.GetAllAsync(s =>
                        s.WorkDate == date &&
                        s.Status == ShiftStatus.CO_SAN.ToString() &&
                        string.IsNullOrEmpty(s.Vehicle_Id));

                    var validQueue = new Queue<Shifts>();
                    foreach (var sh in rawShifts)
                    {
                        var collector = await _unitOfWork.Users.GetByIdAsync(sh.CollectorId);
                        if (collector != null && collector.SmallCollectionPointId == request.CollectionPointId)
                        {
                            validQueue.Enqueue(sh);
                        }
                    }
                    availableShiftQueues[date] = validQueue;
                    response.Logs.Add($"[CHECK SHIFT] Ngày {date}: Tìm thấy {validQueue.Count} tài xế (Shift) rảnh.");
                }

                var vehicleAssignments = staging
                    .GroupBy(s => new { s.VehicleId, s.Date })
                    .ToList();

                response.Logs.Add($"[ANALYSIS] Phân tích dữ liệu: Cần chạy lộ trình cho {vehicleAssignments.Count} xe.");
                foreach (var v in vehicleAssignments)
                {
                    response.Logs.Add($"   -> Xe ID: {v.Key.VehicleId} (Ngày: {v.Key.Date}) - SL Sản phẩm: {v.SelectMany(x => x.ProductIds).Distinct().Count()}");
                }

                int groupCounter = 1;
                var attMap = await GetAttributeIdMapAsync();

                foreach (var grp in vehicleAssignments)
                {
                    var vehicleId = grp.Key.VehicleId;
                    var workDate = grp.Key.Date;

                    response.Logs.Add($"--- Bắt đầu xử lý Xe {vehicleId} ---");

                    try
                    {
                        var allProductIds = grp.SelectMany(x => x.ProductIds).Distinct().ToList();

                        var posts = new List<Post>();
                        foreach (var pid in allProductIds)
                        {
                            var p = await _unitOfWork.Posts.GetAsync(x => x.ProductId == pid);
                            if (p != null) posts.Add(p);
                        }

                        if (!posts.Any())
                        {
                            var msg = $"[LỖI DỮ LIỆU] Xe {vehicleId}: Có {allProductIds.Count} ID sản phẩm nhưng không tìm thấy Post nào trong DB.";
                            response.Errors.Add(msg);
                            response.Logs.Add(msg);
                            continue;
                        }

                        response.Logs.Add($"   + Đã lấy được {posts.Count} bài đăng hợp lệ.");

                        Shifts mainShift = null;
                        var assignedShift = await _unitOfWork.Shifts.GetAsync(s => s.WorkDate == workDate && s.Vehicle_Id == vehicleId);

                        if (assignedShift != null)
                        {
                            mainShift = assignedShift;
                            response.Logs.Add($"   + Dùng lại Shift đã gán trước đó (ID: {mainShift.ShiftId})");
                        }
                        else
                        {
                            if (availableShiftQueues.ContainsKey(workDate) && availableShiftQueues[workDate].Count > 0)
                            {
                                var selectedShift = availableShiftQueues[workDate].Dequeue();
                                selectedShift.Vehicle_Id = vehicleId;
                                selectedShift.Status = ShiftStatus.DA_LEN_LICH.ToString();
                                selectedShift.WorkDate = workDate;
                                mainShift = selectedShift;
                                _unitOfWork.Shifts.Update(mainShift);

                                response.Logs.Add($"   + Gán thành công Shift mới (ID: {mainShift.ShiftId}). Còn lại trong hàng đợi: {availableShiftQueues[workDate].Count}");
                            }
                            else
                            {
                                var msg = $"[LỖI TÀI XẾ] Xe {vehicleId}: Hết tài xế (Shift) rảnh cho ngày {workDate}.";
                                response.Errors.Add(msg);
                                response.Logs.Add(msg);
                                continue;
                            }
                        }

                        var oldGroups = await _unitOfWork.CollectionGroupGeneric.GetAllAsync(g => g.Shift_Id == mainShift.ShiftId);
                        if (oldGroups.Any()) response.Logs.Add($"   + Xóa {oldGroups.Count()} group cũ của Shift này.");

                        foreach (var g in oldGroups)
                        {
                            var routes = await _unitOfWork.CollecctionRoutes.GetAllAsync(r => r.CollectionGroupId == g.CollectionGroupId);
                            foreach (var r in routes) _unitOfWork.CollecctionRoutes.Delete(r);
                            _unitOfWork.CollectionGroupGeneric.Delete(g);
                        }

                        var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(vehicleId);
                        if (vehicle == null)
                        {
                            response.Errors.Add($"Không tìm thấy thông tin xe ID: {vehicleId}");
                            continue;
                        }

                        var locations = new List<(double lat, double lng)>();
                        var nodesToOptimize = new List<OptimizationNode>();
                        var mapData = new List<dynamic>();

                        locations.Add((point.Latitude, point.Longitude));

                        var shiftStart = TimeOnly.FromDateTime(mainShift.Shift_Start_Time.AddHours(7));
                        var shiftEnd = TimeOnly.FromDateTime(mainShift.Shift_End_Time.AddHours(7));

                        foreach (var p in posts)
                        {
                            double lat = point.Latitude, lng = point.Longitude;
                            string displayAddress = p.Address ?? "N/A";
                            if (!string.IsNullOrEmpty(p.Address))
                            {
                                var matchedAddress = await _unitOfWork.UserAddresses.GetAsync(a => a.UserId == p.SenderId && a.Address == p.Address);
                                if (matchedAddress != null && matchedAddress.Iat.HasValue && matchedAddress.Ing.HasValue && Math.Abs(matchedAddress.Iat.Value) > 0.0001)
                                {
                                    lat = matchedAddress.Iat.Value; lng = matchedAddress.Ing.Value;
                                }
                            }

                            TimeOnly finalStart = shiftStart, finalEnd = shiftEnd;
                            if (TryGetTimeWindowForDate(p.ScheduleJson!, workDate, out var st, out var en))
                            {
                                var cS = st < shiftStart ? shiftStart : st;
                                var cE = en > shiftEnd ? shiftEnd : en;
                                if (cS < cE) { finalStart = cS; finalEnd = cE; }
                            }

                            var metrics = await GetProductMetricsInternalAsync(p.ProductId, attMap);

                            locations.Add((lat, lng));
                            nodesToOptimize.Add(new OptimizationNode { OriginalIndex = mapData.Count, Weight = metrics.weight, Volume = metrics.volume, Start = finalStart, End = finalEnd });

                            var user = await _unitOfWork.Users.GetByIdAsync(p.SenderId);
                            var product = await _unitOfWork.Products.GetByIdAsync(p.ProductId);
                            var cat = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId);
                            var brand = await _unitOfWork.Brands.GetByIdAsync(product.BrandId);

                            mapData.Add(new { Post = p, User = user, DisplayAddress = displayAddress, CategoryName = cat?.Name, BrandName = brand?.Name, Att = new { metrics.weight, metrics.volume, DimensionText = $"{metrics.length}x{metrics.width}x{metrics.height}" } });
                        }

                        if (!nodesToOptimize.Any())
                        {
                            response.Errors.Add($"Xe {vehicleId}: Không có dữ liệu tối ưu (Nodes = 0).");
                            continue;
                        }

                        response.Logs.Add($"   + Chuẩn bị chạy thuật toán VRP cho {nodesToOptimize.Count} điểm giao hàng.");

                        int locCount = locations.Count;
                        long[,] matrixDist = new long[locCount, locCount];
                        long[,] matrixTime = new long[locCount, locCount];
                        double speed = 30.0 * 1000 / 3600;

                        for (int i = 0; i < locCount; i++)
                        {
                            for (int j = 0; j < locCount; j++)
                            {
                                if (i == j) continue;
                                double d = GeoHelper.DistanceKm(locations[i].lat, locations[i].lng, locations[j].lat, locations[j].lng);
                                long dm = (long)(d * 1000 * 1.25);
                                matrixDist[i, j] = dm;
                                matrixTime[i, j] = (long)(dm / speed);
                            }
                        }

                        double vehicleVol = vehicle.Length_M * vehicle.Width_M * vehicle.Height_M;
                        var sortedIndices = RouteOptimizer.SolveVRP(matrixDist, matrixTime, nodesToOptimize, vehicle.Capacity_Kg, vehicleVol, shiftStart, shiftEnd);

                        response.Logs.Add($"   + Thuật toán VRP hoàn tất. Kết quả: {sortedIndices.Count} điểm.");

                        var group = new CollectionGroups
                        {
                            Group_Code = $"GRP-{workDate:MMdd}-{groupCounter++}",
                            Shift_Id = mainShift.ShiftId,
                            Name = $"{vehicle.Vehicle_Type} - {vehicle.Plate_Number}",
                            Created_At = DateTime.UtcNow.AddHours(7)
                        };

                        if (request.SaveResult) { await _unitOfWork.CollectionGroupGeneric.AddAsync(group); await _unitOfWork.SaveAsync(); }

                        var routeNodes = new List<RouteDetail>();
                        TimeOnly cursorTime = shiftStart;
                        int prevLocIdx = 0;
                        double totalKg = 0, totalM3 = 0;

                        for (int i = 0; i < sortedIndices.Count; i++)
                        {
                            int idx = sortedIndices[i];
                            int cIdx = idx + 1;
                            var data = mapData[idx];

                            if (request.SaveResult)
                            {
                                var prodToUp = await _unitOfWork.Products.GetByIdAsync((Guid)data.Post.ProductId);
                                if (prodToUp != null)
                                {
                                    prodToUp.Status = ProductStatus.CHO_THU_GOM.ToString();
                                    _unitOfWork.Products.Update(prodToUp);
                                }
                            }

                            long tSec = matrixTime[prevLocIdx, cIdx];
                            var arr = cursorTime.AddMinutes(tSec / 60.0);
                            if (arr < nodesToOptimize[idx].Start) arr = nodesToOptimize[idx].Start;
                            bool isLate = arr > nodesToOptimize[idx].End;

                            routeNodes.Add(new RouteDetail
                            {
                                PickupOrder = i + 1,
                                ProductId = data.Post.ProductId,
                                UserName = data.User.Name,
                                Address = data.DisplayAddress,
                                DistanceKm = Math.Round(matrixDist[prevLocIdx, cIdx] / 1000.0, 2),
                                EstimatedArrival = arr.ToString("HH:mm") + (isLate ? " (Trễ)" : ""),
                                IsLate = isLate,
                                Schedule = JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>((string)data.Post.ScheduleJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
                                CategoryName = data.CategoryName ?? "N/A",
                                BrandName = data.BrandName ?? "N/A",
                                DimensionText = data.Att.DimensionText,
                                WeightKg = data.Att.weight,
                                VolumeM3 = data.Att.volume
                            });

                            if (request.SaveResult)
                            {
                                await _unitOfWork.CollecctionRoutes.AddAsync(new CollectionRoutes { CollectionGroupId = group.CollectionGroupId, ProductId = data.Post.ProductId, CollectionDate = workDate, EstimatedTime = arr, DistanceKm = Math.Round(matrixDist[prevLocIdx, cIdx] / 1000.0, 2), Status = "CHUA_BAT_DAU", ConfirmImages = new List<string>() });
                            }

                            cursorTime = arr.AddMinutes(serviceTime);
                            prevLocIdx = cIdx;
                            totalKg += (double)data.Att.weight; totalM3 += (double)data.Att.volume;
                        }

                        if (request.SaveResult) await _unitOfWork.SaveAsync();

                        var colObj = await _unitOfWork.Users.GetByIdAsync(mainShift.CollectorId);
                        response.CreatedGroups.Add(new GroupSummary
                        {
                            GroupId = group.CollectionGroupId,
                            GroupCode = group.Group_Code,
                            Collector = colObj?.Name,
                            Vehicle = $"{vehicle.Plate_Number} ({vehicle.Vehicle_Type})",
                            ShiftId = mainShift.ShiftId,
                            GroupDate = workDate,
                            TotalPosts = routeNodes.Count,
                            TotalWeightKg = Math.Round(totalKg, 2),
                            TotalVolumeM3 = Math.Round(totalM3, 3),
                            Routes = routeNodes
                        });

                        response.Logs.Add($"[SUCCESS] Đã tạo xong Group cho xe {vehicleId}.");
                    }
                    catch (Exception ex)
                    {
                        response.Errors.Add($"[EXCEPTION] Xe {vehicleId}: {ex.Message}");
                        response.Logs.Add($"[EXCEPTION] {ex.ToString()}");
                    }
                } 

                if (response.CreatedGroups.Count == 0 && response.Errors.Count == 0)
                {
                    response.Logs.Add("[WARNING] Kết thúc mà không tạo được group nào và không có lỗi (Vòng lặp không chạy?)");
                }
            }
            catch (Exception ex)
            {
                response.Errors.Add($"[SYSTEM ERROR] {ex.Message}");
            }

            return response;
        }

        public async Task<PagedResult<CollectionGroupModel>> GetGroupsByCollectionPointAsync( string collectionPointId, int page, int limit)
        {
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(collectionPointId);
            if (point == null)
                throw new Exception("Trạm thu gom không tồn tại.");

            var attMap = await GetAttributeIdMapAsync();

            var (groups, totalCount) =
                await _unitOfWork.CollectionGroups.GetPagedGroupsByCollectionPointAsync( collectionPointId, page, limit);

            var resultItems = new List<CollectionGroupModel>();

            foreach (var group in groups)
            {
                var routes = await _unitOfWork.CollecctionRoutes
                    .GetAllAsync(r => r.CollectionGroupId == group.CollectionGroupId);

                double totalW = 0;
                double totalV = 0;

                foreach (var r in routes)
                {
                    var metrics = await GetProductMetricsInternalAsync(r.ProductId, attMap);
                    totalW += metrics.weight;
                    totalV += metrics.volume;
                }

                resultItems.Add(new CollectionGroupModel
                {
                    GroupId = group.CollectionGroupId,
                    GroupCode = group.Group_Code,
                    ShiftId = group.Shift_Id,
                    Vehicle = group.Shifts.Vehicle != null
                        ? $"{group.Shifts.Vehicle.Plate_Number} ({group.Shifts.Vehicle.Vehicle_Type})"
                        : "Không rõ",
                    Collector = group.Shifts.Collector?.Name ?? "Không rõ",
                    Date = group.Shifts.WorkDate.ToString("yyyy-MM-dd"),
                    TotalOrders = routes.Count(),
                    TotalWeightKg = Math.Round(totalW, 2),
                    TotalVolumeM3 = Math.Round(totalV, 4),
                    CreatedAt = group.Created_At
                });
            }

            return new PagedResult<CollectionGroupModel>
            {
                Data = resultItems,
                TotalItems = totalCount,
                Page = page,
                Limit = limit
            };
        }

        public async Task<object> GetRoutesByGroupAsync(int groupId, int page, int limit)
        {
            if (page <= 0) page = 1;
            if (limit <= 0) limit = 10;

            var group = await _unitOfWork.CollectionGroupGeneric.GetByIdAsync(groupId)
                ?? throw new Exception("Không tìm thấy group.");

            var shift = await _unitOfWork.Shifts.GetByIdAsync(group.Shift_Id);

            var allRoutes = await _unitOfWork.CollecctionRoutes
                .GetAllAsync(r => r.CollectionGroupId == groupId);

            var totalProduct = allRoutes.Count();
            if (totalProduct == 0)
                throw new Exception("Group không có route nào.");

            double totalWeightAll = 0;
            double totalVolumeAll = 0;
            var attMap = await GetAttributeIdMapAsync();

            foreach (var r in allRoutes)
            {
                var m = await GetProductMetricsInternalAsync(r.ProductId, attMap);
                totalWeightAll += m.weight;
                totalVolumeAll += m.volume;
            }

            var pagedRoutes = allRoutes
                .OrderBy(r => r.EstimatedTime)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            var totalPage = (int)Math.Ceiling((double)totalProduct / limit);

            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(shift.Vehicle_Id);
            var collector = await _unitOfWork.Users.GetByIdAsync(shift.CollectorId);
            string pointId = vehicle?.Small_Collection_Point ?? collector?.SmallCollectionPointId;
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(pointId);

            int order = (page - 1) * limit + 1;
            var routeList = new List<object>();

            foreach (var r in pagedRoutes)
            {
                var post = await _unitOfWork.Posts.GetAsync(p => p.ProductId == r.ProductId);
                if (post == null) continue;

                var user = await _unitOfWork.Users.GetByIdAsync(post.SenderId);
                var product = post.Product ?? await _unitOfWork.Products.GetByIdAsync(r.ProductId);
                var category = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId);
                var brand = await _unitOfWork.Brands.GetByIdAsync(product.BrandId);

                var metrics = await GetProductMetricsInternalAsync(post.ProductId, attMap);

                routeList.Add(new
                {
                    pickupOrder = order++,
                    productId = post.ProductId,
                    postId = post.PostId,
                    userName = user?.Name ?? "N/A",
                    address = post.Address ?? "Không có",
                    categoryName = category?.Name ?? "Không rõ",
                    brandName = brand?.Name ?? "Không rõ",
                    dimensionText = $"{metrics.length} x {metrics.width} x {metrics.height}",
                    weightKg = metrics.weight,
                    volumeM3 = Math.Round(metrics.volume, 4),
                    distanceKm = r.DistanceKm,
                    schedule = JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>(
                        post.ScheduleJson!,
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
                totalProduct,
                totalPage,
                page,
                limit,
                totalWeightKg = Math.Round(totalWeightAll, 2),
                totalVolumeM3 = Math.Round(totalVolumeAll, 4),
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

        public async Task<PagedCompanySettingsResponse> GetCompanySettingsPagedAsync( string companyId, int page, int limit)
        {
            if (page <= 0) page = 1;
            if (limit <= 0) limit = 10;

            var company = await _unitOfWork.Companies.GetByIdAsync(companyId)
                ?? throw new Exception($"Không tìm thấy công ty với ID: {companyId}");

            var pointQuery = _unitOfWork.SmallCollectionPoints
                .AsQueryable()
                .Where(p => p.CompanyId == companyId);

            var totalItems = await pointQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)limit);

            var points = await pointQuery
                .OrderBy(p => p.Name)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var configs = await _unitOfWork.SystemConfig.GetAllAsync(c =>
                c.CompanyId == companyId || c.SmallCollectionPointId != null);

            return new PagedCompanySettingsResponse
            {
                CompanyId = company.CompanyId,
                CompanyName = company.Name,
                Page = page,
                Limit = limit,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Points = points.Select(p => new PointSettingDetailDto
                {
                    SmallPointId = p.SmallCollectionPointsId,
                    SmallPointName = p.Name,
                    ServiceTimeMinutes = GetConfigValue(
                        configs,
                        null,
                        p.SmallCollectionPointsId,
                        SystemConfigKey.SERVICE_TIME_MINUTES,
                        DEFAULT_SERVICE_TIME),

                    AvgTravelTimeMinutes = GetConfigValue(
                        configs,
                        null,
                        p.SmallCollectionPointsId,
                        SystemConfigKey.AVG_TRAVEL_TIME_MINUTES,
                        DEFAULT_TRAVEL_TIME),

                    IsDefault = false
                }).ToList()
            };
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

        private class VehicleBucket
        {
            public Vehicles Vehicle { get; set; }
            public double CurrentTimeMin { get; set; }
            public double CurrentKg { get; set; }
            public double CurrentM3 { get; set; }
            public double MaxKg { get; set; }
            public double MaxM3 { get; set; }
            public List<PreAssignProduct> Products { get; set; }
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

        private static bool TryGetTimeWindowForDate(string raw, DateOnly targetDate, out TimeOnly start, out TimeOnly end)
        {
            // Mặc định: 7h sáng đến 9h tối nếu không parse được
            start = new TimeOnly(7, 0);
            end = new TimeOnly(21, 0);

            if (string.IsNullOrWhiteSpace(raw)) return false;

            try
            {
                // Cấu hình Case Insensitive để đọc được cả "startTime" lẫn "StartTime"
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var days = JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>(raw, opts);

                if (days == null) return false;

                // Tìm đúng ngày
                var match = days.FirstOrDefault(d =>
                    DateOnly.TryParse(d.PickUpDate, out var dt) && dt == targetDate);

                if (match?.Slots != null)
                {
                    bool hasStart = TimeOnly.TryParse(match.Slots.StartTime, out var s);
                    bool hasEnd = TimeOnly.TryParse(match.Slots.EndTime, out var e);

                    if (hasStart) start = s;
                    if (hasEnd) end = e;

                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
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
                    if (DateOnly.TryParse(d.PickUpDate, out var date))
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
