using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Application.Interfaces;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.GroupModel;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace ElecWasteCollection.Application.Services
{
    public class GroupingService : IGroupingService
    {

        private const double DEFAULT_SERVICE_TIME = 10;
        private const double DEFAULT_SPEED_KMH = 25; 
        private const double DETOUR_FACTOR = 1.3;

        private static List<StagingDataModel> _inMemoryStaging = new();
        private static readonly object _lockObj = new object();

        private static readonly List<PreAssignResponseCache> _preAssignPreviewCache = new();
        private static readonly object _previewLock = new();


        private readonly ICollectionGroupRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public GroupingService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;

        }
        public async Task<PreAssignResponse> PreAssignAsync(PreAssignRequest request)
        {
            lock (_previewLock)
            {
                _preAssignPreviewCache.RemoveAll(x => x.CachedAt < DateTime.UtcNow.AddMinutes(-15));
            }

            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(request.CollectionPointId)
                ?? throw new Exception("Không tìm thấy trạm thu gom.");

            if (point.Latitude == 0 || point.Longitude == 0)
                throw new Exception("Trạm thu gom chưa cấu hình tọa độ GPS. Không thể tính toán lộ trình VRP.");

            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync(c => c.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString());
            double avgSpeedKmH = GetConfigValue(allConfigs, point.CompanyId, point.SmallCollectionPointsId.ToString(), SystemConfigKey.TRANSPORT_SPEED, 25);
            double serviceTimeMin = GetConfigValue(allConfigs, point.CompanyId, point.SmallCollectionPointsId.ToString(), SystemConfigKey.SERVICE_TIME_MINUTES, 10);

            var vehicles = (await _unitOfWork.Vehicles.GetAllAsync(v =>
                request.VehicleIds.Contains(v.VehicleId) &&
                v.Status == VehicleStatus.DANG_HOAT_DONG.ToString())).OrderBy(v => v.Capacity_Kg).ToList();

            var shift = (await _unitOfWork.Shifts.GetAllAsync(s =>
                s.WorkDate == request.WorkDate &&
                s.Status == ShiftStatus.CO_SAN.ToString() &&
                s.Collector.SmallCollectionPointId == request.CollectionPointId, includeProperties: "Collector")).FirstOrDefault()
                ?? throw new Exception($"Không có ca làm việc trống ngày {request.WorkDate:dd/MM/yyyy}");

            double totalShiftMin = (shift.Shift_End_Time.ToLocalTime() - shift.Shift_Start_Time.ToLocalTime()).TotalMinutes;
            TimeOnly shiftStart = TimeOnly.FromDateTime(shift.Shift_Start_Time.ToLocalTime());
            TimeOnly shiftEnd = TimeOnly.FromDateTime(shift.Shift_End_Time.ToLocalTime());

            var attIdMap = await GetAttributeIdMapAsync();
            var rawPosts = await _unitOfWork.Posts.GetAllAsync(p =>
                p.AssignedSmallPointId == request.CollectionPointId && request.ProductIds.Contains(p.ProductId),
                includeProperties: "Product,Product.User,Product.Category,Product.Brand");

            var pool = new List<dynamic>();
            foreach (var p in rawPosts.Where(x => x.Product?.Status == ProductStatus.CHO_GOM_NHOM.ToString()))
            {
                if (!TryParseScheduleInfo(p.ScheduleJson!, out var sch) || !((List<DateOnly>)sch.SpecificDates).Contains(request.WorkDate)) continue;
                if (!TryGetTimeWindowForDate(p.ScheduleJson!, request.WorkDate, out var custStart, out var custEnd)) continue;

                var addr = await _unitOfWork.UserAddresses.GetAsync(a => a.UserId == p.SenderId && a.Address == p.Address);
                if (addr?.Iat == null || addr.Iat == 0) continue;

                var metrics = await GetProductMetricsInternalAsync(p.ProductId, attIdMap);
                pool.Add(new
                {
                    Post = p,
                    Lat = addr.Iat.Value,
                    Lng = addr.Ing.Value,
                    Weight = metrics.weight,
                    Volume = metrics.volume,
                    IsCritical = ((List<DateOnly>)sch.SpecificDates).Max() <= request.WorkDate,
                    CustStart = custStart,
                    CustEnd = custEnd,
                    UserName = p.Product?.User?.Name ?? "N/A",
                    FullAddress = p.Address ?? "N/A",
                    CategoryName = p.Product?.Category?.Name ?? "N/A",
                    BrandName = p.Product?.Brand?.Name ?? "N/A",
                    DimText = $"{Math.Round(metrics.length * 100)}x{Math.Round(metrics.width * 100)}x{Math.Round(metrics.height * 100)} cm"
                });
            }

            double loadFactor = request.LoadThresholdPercent > 0 ? request.LoadThresholdPercent / 100.0 : 1.0;
            var buckets = vehicles.ToDictionary(v => v.VehicleId, v => new VehicleBucket
            {
                Vehicle = v,
                LastLat = point.Latitude,
                LastLng = point.Longitude,
                MaxKg = v.Capacity_Kg * loadFactor,
                MaxM3 = (v.Length_M * v.Width_M * v.Height_M) * loadFactor,
                MaxShiftMinutes = totalShiftMin,
                ShiftStartBase = shiftStart
            });

            var unAssigned = new List<UnAssignProductPreview>();

            var sortedPool = pool.OrderByDescending(x => x.IsCritical).ThenByDescending(x => x.Weight).ToList();
            foreach (var item in sortedPool)
            {
                if (!TryAssignToBucket(item, buckets, point, avgSpeedKmH, serviceTimeMin))
                {
                    unAssigned.Add(new UnAssignProductPreview
                    {
                        ProductId = item.Post.ProductId.ToString(),
                        PostId = item.Post.PostId.ToString(),
                        Name = item.UserName,
                        Address = item.FullAddress,
                        Weight = Math.Round(item.Weight, 2),
                        Volume = Math.Round(item.Volume, 4),
                        CategoryName = item.CategoryName,
                        BrandName = item.BrandName,
                        DimensionText = item.DimText,
                        Reason = item.IsCritical ? "HẠN CHÓT - Không tìm được xe kịp giờ hoặc tải trọng." : "Xe đã đầy hoặc hết ca làm việc."
                    });
                }
            }

            foreach (var b in buckets.Values.Where(v => v.Products.Any()))
            {
                var nodesForVRP = b.Products.Select(p => {
                    var originalItem = pool.First(x => x.Post.ProductId.ToString() == p.ProductId);
                    return new OptimizationNode
                    {
                        OriginalIndex = b.Products.IndexOf(p),
                        Weight = originalItem.Weight,
                        Volume = originalItem.Volume,
                        Start = originalItem.CustStart,
                        End = originalItem.CustEnd,
                        Lat = originalItem.Lat,
                        Lng = originalItem.Lng
                    };
                }).ToList();

                var matrix = BuildMatrixForVehicle(point.Latitude, point.Longitude, nodesForVRP, avgSpeedKmH);

                var optimizedOrder = RouteOptimizer.SolveVRP(
                    matrix.Distances, matrix.Times, nodesForVRP,
                    b.Vehicle.Capacity_Kg, (b.Vehicle.Length_M * b.Vehicle.Width_M * b.Vehicle.Height_M),
                    shiftStart, shiftEnd);

                b.Products = ReorderAndRecalculate(optimizedOrder, b, nodesForVRP, shiftStart, avgSpeedKmH, serviceTimeMin, point);

                b.CurrentKg = b.Products.Sum(p => p.Weight);
                b.CurrentM3 = b.Products.Sum(p => p.Volume);
            }

            var result = new PreAssignResponse
            {
                SmallCollectionPointId = request.CollectionPointId,
                CollectionPoint = point.Name,
                WorkDate = request.WorkDate,
                LoadThresholdPercent = request.LoadThresholdPercent,
                UnassignedProducts = unAssigned,
                Days = buckets.Values.Where(b => b.Products.Any()).Select(b => new PreAssignDay
                {
                    WorkDate = request.WorkDate,
                    TotalWeight = Math.Round(b.CurrentKg, 2),
                    TotalVolume = Math.Round(b.CurrentM3, 4),
                    OriginalPostCount = b.Products.Count,
                    SuggestedVehicle = new SuggestedVehicle
                    {
                        Id = b.Vehicle.VehicleId.ToString(),
                        Plate_Number = b.Vehicle.Plate_Number,
                        Vehicle_Type = b.Vehicle.Vehicle_Type,
                        Capacity_Kg = b.Vehicle.Capacity_Kg,
                        Capacity_M3 = Math.Round(b.Vehicle.Length_M * b.Vehicle.Width_M * b.Vehicle.Height_M, 4),
                        AllowedCapacityKg = Math.Round(b.MaxKg, 2),
                        AllowedCapacityM3 = Math.Round(b.MaxM3, 4)
                    },
                    Products = b.Products
                }).ToList()
            };

            lock (_previewLock)
            {
                _preAssignPreviewCache.RemoveAll(x => x.SmallCollectionPointId == request.CollectionPointId && x.WorkDate == request.WorkDate);
                _preAssignPreviewCache.Add(new PreAssignResponseCache
                {
                    SmallCollectionPointId = request.CollectionPointId,
                    WorkDate = request.WorkDate,
                    CachedAt = DateTime.UtcNow,
                    Response = result
                });
            }

            return result;
        }
        public async Task<bool> AssignDayAsync(AssignDayRequest request)
        {
            if (request == null) throw new Exception("Yêu cầu không được để trống.");
            if (string.IsNullOrEmpty(request.CollectionPointId)) throw new Exception("ID trạm thu gom không hợp lệ.");
            if (request.Assignments == null || !request.Assignments.Any())
                throw new Exception("Danh sách điều phối xe trống.");

            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(request.CollectionPointId);
            if (point == null)
                throw new Exception($"Trạm thu gom không tồn tại (ID: {request.CollectionPointId})");

            var cachedResult = _preAssignPreviewCache.FirstOrDefault(x =>
                x.SmallCollectionPointId == request.CollectionPointId &&
                x.WorkDate == request.WorkDate)?.Response;

            foreach (var item in request.Assignments)
            {
                if (item.ProductIds == null || !item.ProductIds.Any())
                    throw new Exception($"Xe {item.VehicleId}: Danh sách ProductIds không được để trống.");

                var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(item.VehicleId)
                    ?? throw new Exception($"Xe {item.VehicleId} không tồn tại trong hệ thống.");

                if (vehicle.Small_Collection_Point != request.CollectionPointId)
                    throw new Exception($"Xe {vehicle.Plate_Number} không thuộc quyền quản lý của trạm này.");

                lock (_lockObj)
                {
                    var busyOtherPoint = _inMemoryStaging.Any(s =>
                        s.Date == request.WorkDate &&
                        s.VehicleId == item.VehicleId &&
                        s.PointId != request.CollectionPointId);

                    if (busyOtherPoint)
                        throw new Exception($"Xe {vehicle.Plate_Number} đã được điều phối tại trạm khác vào ngày {request.WorkDate:dd/MM/yyyy}.");
                }
            }

            lock (_lockObj)
            {
                foreach (var item in request.Assignments)
                {
                    _inMemoryStaging.RemoveAll(s =>
                        s.Date == request.WorkDate &&
                        s.VehicleId == item.VehicleId);

                    var details = new List<ProductStagingDetail>();
                    foreach (var pid in item.ProductIds)
                    {
                        var cachedProd = cachedResult?.Days
                            .SelectMany(d => d.Products)
                            .FirstOrDefault(p => p.ProductId == pid);

                        details.Add(new ProductStagingDetail
                        {
                            ProductId = Guid.Parse(pid),
                            DistanceKm = cachedProd?.DistanceKm ?? 0,
                            EstimatedArrival = cachedProd?.EstimatedArrival ?? "--:--"
                        });
                    }

                    _inMemoryStaging.Add(new StagingDataModel
                    {
                        StagingId = Guid.NewGuid(),
                        Date = request.WorkDate,
                        PointId = request.CollectionPointId,
                        VehicleId = item.VehicleId,
                        ProductDetails = details
                    });

                    Console.WriteLine($"Đã lưu thành công {details.Count} sản phẩm cho xe {item.VehicleId}.");
                }

                var totalVehicles = _inMemoryStaging.Count(s => s.Date == request.WorkDate && s.PointId == request.CollectionPointId);
                Console.WriteLine($"Trạm {request.CollectionPointId} đang giữ {totalVehicles} xe cho ngày {request.WorkDate:dd/MM/yyyy}.");
            }

            return await Task.FromResult(true);
        }

        public async Task<GroupingByPointResponse> GroupByCollectionPointAsync(GroupingByPointRequest request)
        {
            // 1. Lấy thông tin trạm thu gom và công ty quản lý
            var point = await _unitOfWork.SmallCollectionPoints.GetAsync(
                p => p.SmallCollectionPointsId == request.CollectionPointId,
                includeProperties: "CollectionCompany")
                ?? throw new Exception($"Không tìm thấy trạm thu gom với ID: {request.CollectionPointId}");

            var response = new GroupingByPointResponse
            {
                CollectionPoint = point.Name,
                SavedToDatabase = request.SaveResult,
                CreatedGroups = new List<GroupSummary>()
            };

            // 2. Lấy dữ liệu đã điều phối từ bộ nhớ tạm (Staging)
            var stagingData = _inMemoryStaging.Where(s => s.PointId == request.CollectionPointId).ToList();
            if (!stagingData.Any())
                throw new Exception("Không có dữ liệu điều phối trong bộ nhớ tạm.");

            var attIdMap = await GetAttributeIdMapAsync();
            string companyCode = GetCompanyInitials(point.CollectionCompany?.Name ?? "CORP");

            foreach (var stage in stagingData)
            {
                var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(stage.VehicleId);
                var shift = await FindAndAssignUniqueShiftAsync(stage.VehicleId.ToString(), stage.Date, request.CollectionPointId);
                if (shift == null) continue;

                // 3. Khởi tạo thực thể Group
                string cleanPlate = vehicle.Plate_Number.Replace("-", "").Replace(".", "").ToUpper();
                var group = new CollectionGroups
                {
                    Group_Code = $"{companyCode}-{request.CollectionPointId}-{stage.Date:MMdd}-{cleanPlate}",
                    Shift_Id = shift.ShiftId,
                    Name = $"{vehicle.Plate_Number} Route",
                    Created_At = DateTime.UtcNow.AddHours(7)
                };

                var groupSummary = new GroupSummary
                {
                    GroupCode = group.Group_Code,
                    Vehicle = vehicle.Plate_Number,
                    ShiftId = shift.ShiftId.ToString(),
                    Collector = shift.Collector?.Name ?? "N/A",
                    GroupDate = stage.Date,
                    TotalPosts = stage.ProductDetails.Count,
                    Routes = new List<RouteDetail>()
                };

                double totalWeight = 0;
                double totalVolume = 0;

                // Lưu Group trước để lấy ID cho các Route chi tiết
                if (request.SaveResult)
                {
                    await _unitOfWork.CollectionGroupGeneric.AddAsync(group);
                    await _unitOfWork.SaveAsync();
                }

                // 4. Xử lý từng điểm thu gom trong lộ trình
                for (int i = 0; i < stage.ProductDetails.Count; i++)
                {
                    var detail = stage.ProductDetails[i];

                    var prod = await _unitOfWork.Products.GetAsync(p => p.ProductId == detail.ProductId,
                        includeProperties: "User,Category,Brand");

                    var post = await _unitOfWork.Posts.GetAsync(p => p.ProductId == detail.ProductId);
                    var metrics = await GetProductMetricsInternalAsync(detail.ProductId, attIdMap);

                    totalWeight += metrics.weight;
                    totalVolume += metrics.volume;

                    // Mapping dữ liệu cho kết quả trả về (Preview/Response)
                    groupSummary.Routes.Add(new RouteDetail
                    {
                        PickupOrder = i + 1,
                        ProductId = detail.ProductId,
                        UserName = prod?.User?.Name ?? "N/A",
                        Address = post?.Address ?? "N/A",
                        BrandName = prod?.Brand?.Name ?? "N/A",
                        CategoryName = prod?.Category?.Name ?? "N/A",
                        WeightKg = Math.Round(metrics.weight, 2),
                        VolumeM3 = Math.Round(metrics.volume, 4),
                        DimensionText = $"{Math.Round(metrics.length * 100)}x{Math.Round(metrics.width * 100)}x{Math.Round(metrics.height * 100)} cm",
                        DistanceKm = detail.DistanceKm,
                        EstimatedArrival = detail.EstimatedArrival,
                        Schedule = post?.ScheduleJson ?? "",
                        IsLate = false
                    });

                    // 5. Lưu chi tiết lộ trình xuống Database
                    if (request.SaveResult)
                    {
                        if (prod != null)
                        {
                            prod.Status = ProductStatus.CHO_THU_GOM.ToString();
                            _unitOfWork.Products.Update(prod);
                        }

                        // Parse EstimatedArrival (string "HH:mm") sang TimeOnly
                        TimeOnly estimatedTimeValue = TimeOnly.MinValue;
                        if (TimeOnly.TryParseExact(detail.EstimatedArrival, "HH:mm", out var parsedTime))
                        {
                            estimatedTimeValue = parsedTime;
                        }

                        await _unitOfWork.CollecctionRoutes.AddAsync(new CollectionRoutes
                        {
                            CollectionGroupId = group.CollectionGroupId,
                            ProductId = detail.ProductId,
                            CollectionDate = stage.Date,
                            DistanceKm = (double)detail.DistanceKm,
                            EstimatedTime = estimatedTimeValue, 
                            Status = CollectionRouteStatus.CHUA_BAT_DAU.ToString()
                        });
                    }
                }

                groupSummary.GroupId = group.CollectionGroupId;
                groupSummary.TotalWeightKg = Math.Round(totalWeight, 2);
                groupSummary.TotalVolumeM3 = Math.Round(totalVolume, 4);

                if (request.SaveResult) await _unitOfWork.SaveAsync();
                response.CreatedGroups.Add(groupSummary);
            }

            // 6. Dọn dẹp bộ nhớ tạm sau khi đã lưu thành công
            if (request.SaveResult)
            {
                lock (_lockObj)
                {
                    _inMemoryStaging.RemoveAll(s => s.PointId == request.CollectionPointId);
                }
            }

            return response;
        }

        private string GetCompanyInitials(string companyName)
        {
            if (string.IsNullOrWhiteSpace(companyName)) return "CORP";

            var words = companyName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var initials = words.Select(w => w[0]).Take(3).ToArray();
            return new string(initials).ToUpper();
        }
        private async Task<Shifts> FindAndAssignUniqueShiftAsync(string vehicleId, DateOnly date, string pointId)
        {
            var vehicleIdStr = vehicleId.ToString();
            var pointIdStr = pointId.ToString();

            var existing = await _unitOfWork.Shifts.GetAsync(s =>
                s.WorkDate == date &&
                s.Vehicle_Id == vehicleIdStr);

            if (existing != null) return existing;

            var allShiftsToday = await _unitOfWork.Shifts.GetAllAsync(
                s => s.WorkDate == date,
                includeProperties: "Collector"
            );

            var busyCollectorIds = allShiftsToday
                .Where(s => !string.IsNullOrEmpty(s.Vehicle_Id))
                .Select(s => s.CollectorId)
                .Distinct()
                .ToList();

            var availableShift = allShiftsToday
                .Where(s => s.Status == ShiftStatus.CO_SAN.ToString() &&
                            string.IsNullOrEmpty(s.Vehicle_Id) &&
                            !busyCollectorIds.Contains(s.CollectorId) &&
                            s.Collector != null &&
                            string.Equals(s.Collector.SmallCollectionPointId, pointIdStr, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (availableShift != null)
            {
                availableShift.Vehicle_Id = vehicleIdStr;
                availableShift.Status = ShiftStatus.DA_LEN_LICH.ToString();

                _unitOfWork.Shifts.Update(availableShift);
                await _unitOfWork.SaveAsync();

                return availableShift;
            }

            return null;
        }
        public Task<PreviewProductPagedResult?> GetPreviewProductsAsync(
        string vehicleId, DateOnly workDate, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            lock (_previewLock)
            {
                foreach (var preview in _preAssignPreviewCache)
                {
                    var dayGroup = preview.Response.Days.FirstOrDefault(d =>
                        d.WorkDate == workDate &&
                        d.SuggestedVehicle.Id.Equals(vehicleId,
                            StringComparison.OrdinalIgnoreCase));

                    if (dayGroup == null)
                        continue;

                    var total = dayGroup.Products.Count;

                    return Task.FromResult<PreviewProductPagedResult?>(new PreviewProductPagedResult
                    {
                        VehicleId = dayGroup.SuggestedVehicle.Id,
                        PlateNumber = dayGroup.SuggestedVehicle.Plate_Number,
                        VehicleType = dayGroup.SuggestedVehicle.Vehicle_Type,
                        TotalProduct = total,
                        Page = page,
                        PageSize = pageSize,
                        TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                        Products = dayGroup.Products
                            .Skip((page - 1) * pageSize)
                            .Take(pageSize)
                            .Cast<object>()
                            .ToList()
                    });
                }
            }

            return Task.FromResult<PreviewProductPagedResult?>(null);
        }
        public Task<object> GetPreviewVehiclesAsync(string collectionPointId, DateOnly workDate)
        {
            var result = new List<object>();

            lock (_previewLock)
            {
                var preview = _preAssignPreviewCache.FirstOrDefault(x =>
                    x.SmallCollectionPointId == collectionPointId &&
                    x.WorkDate == workDate);

                if (preview == null)
                    return Task.FromResult<object>(result);

                foreach (var d in preview.Response.Days.Where(d => d.WorkDate == workDate))
                {
                    result.Add(new
                    {
                        VehicleId = d.SuggestedVehicle.Id,
                        PlateNumber = d.SuggestedVehicle.Plate_Number,
                        VehicleType = d.SuggestedVehicle.Vehicle_Type,
                        TotalProduct = d.OriginalPostCount,
                        TotalWeight = d.TotalWeight,
                        TotalVolume = d.TotalVolume,
                        CollectionPoint = preview.Response.CollectionPoint
                    });
                }
            }

            return Task.FromResult<object>(result);
        }
        public Task<object> GetUnassignedProductsAsync(string collectionPointId, DateOnly workDate, int page, int pageSize)
        {
            lock (_previewLock)
            {
                var cache = _preAssignPreviewCache.FirstOrDefault(x =>
                    x.SmallCollectionPointId == collectionPointId &&
                    x.WorkDate == workDate);

                if (cache == null)
                {
                    return Task.FromResult<object>(new
                    {
                        Total = 0,
                        Page = page,
                        PageSize = pageSize,
                        Items = new List<UnAssignProductPreview>()
                    });
                }

                var total = cache.Response.UnassignedProducts.Count;

                var items = cache.Response.UnassignedProducts
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Task.FromResult<object>(new
                {
                    Total = total,
                    Page = page,
                    PageSize = pageSize,
                    Items = items
                });
            }
        }
        public async Task<PagedResult<CollectionGroupModel>> GetGroupsByCollectionPointAsync(string collectionPointId, int page, int limit)
        {
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(collectionPointId);
            if (point == null)
                throw new Exception("Trạm thu gom không tồn tại.");

            var attMap = await GetAttributeIdMapAsync();

            var (groups, totalCount) =
                await _unitOfWork.CollectionGroups.GetPagedGroupsByCollectionPointAsync(collectionPointId, page, limit);

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
                    schedule = System.Text.Json.JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>(
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
        public async Task<PagedCompanySettingsResponse> GetCompanySettingsPagedAsync(string companyId, int page, int limit)
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
                        companyId,
                        p.SmallCollectionPointsId,
                        SystemConfigKey.SERVICE_TIME_MINUTES,
                        DEFAULT_SERVICE_TIME),

                    AvgTravelTimeMinutes = GetConfigValue(
                        configs,
                        companyId,
                        p.SmallCollectionPointsId,
                        SystemConfigKey.AVG_TRAVEL_TIME_MINUTES, 
                        DEFAULT_SPEED_KMH),

                    IsDefault = false
                }).ToList()
            };
        }
        public async Task<SinglePointSettingResponse> GetPointSettingAsync(string pointId)
        {
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(pointId);
            if (point == null) throw new Exception("Trạm thu gom không tồn tại.");

            var company = await _unitOfWork.Companies.GetByIdAsync(point.CompanyId);
            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync(c => c.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString());

            return new SinglePointSettingResponse
            {
                CompanyId = company?.CompanyId ?? "Không rõ",
                CompanyName = company?.Name ?? "Không rõ",
                SmallPointId = point.SmallCollectionPointsId,
                SmallPointName = point.Name,
                ServiceTimeMinutes = GetConfigValue(
                    allConfigs,
                    point.CompanyId,
                    point.SmallCollectionPointsId,
                    SystemConfigKey.SERVICE_TIME_MINUTES,
                    DEFAULT_SERVICE_TIME),

                AvgTravelTimeMinutes = GetConfigValue(
                    allConfigs,
                    point.CompanyId,
                    point.SmallCollectionPointsId,
                    SystemConfigKey.AVG_TRAVEL_TIME_MINUTES,
                    DEFAULT_SPEED_KMH),

                IsDefault = false
            };
        }





        //Helper for Pree-assign
        private (long[,] Distances, long[,] Times) BuildMatrixForVehicle(double depotLat, double depotLng, List<OptimizationNode> nodes, double speed)
        {
            int size = nodes.Count + 1;
            long[,] dists = new long[size, size];
            long[,] times = new long[size, size];

            var points = new List<(double Lat, double Lng)> { (depotLat, depotLng) };
            points.AddRange(nodes.Select(n => (n.Lat, n.Lng)));

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (i == j) continue;
                    double d = CalculateHaversine(points[i].Lat, points[i].Lng, points[j].Lat, points[j].Lng) * 1.3;
                    dists[i, j] = (long)(d * 1000); 
                    times[i, j] = (long)((d / speed) * 3600); 
                }
            }
            return (dists, times);
        }

        private List<PreAssignProduct> ReorderAndRecalculate(List<int> order, VehicleBucket b, List<OptimizationNode> nodes, TimeOnly shiftStart, double speed, double service, dynamic point)
        {
            var finalPath = new List<PreAssignProduct>();
            double totalTimeElapsed = 0;
            double currentLat = point.Latitude;
            double currentLng = point.Longitude;

            foreach (var idx in order)
            {
                var node = nodes[idx];
                var product = b.Products[node.OriginalIndex];

                double segmentDist = CalculateHaversine(currentLat, currentLng, node.Lat, node.Lng) * 1.3;
                double travelMin = (segmentDist / speed) * 60;

                TimeOnly physicalArrival = shiftStart.AddMinutes(totalTimeElapsed + travelMin);

                double waitMin = 0;
                if (physicalArrival < node.Start)
                {
                    waitMin = (node.Start.ToTimeSpan() - physicalArrival.ToTimeSpan()).TotalMinutes;
                }

                product.DistanceKm = Math.Round(segmentDist, 2);
                product.EstimatedArrival = physicalArrival.AddMinutes(waitMin).ToString("HH:mm");

                totalTimeElapsed += (travelMin + waitMin + service);
                currentLat = node.Lat;
                currentLng = node.Lng;

                finalPath.Add(product);
            }
            return finalPath;
        }

        private bool TryAssignToBucket(dynamic item, Dictionary<string, VehicleBucket> buckets, dynamic point, double speed, double service)
        {
            foreach (var b in buckets.Values)
            {
                if (b.CurrentKg + item.Weight <= b.MaxKg && b.CurrentM3 + item.Volume <= b.MaxM3)
                {
                    double distTo = CalculateHaversine(b.LastLat, b.LastLng, item.Lat, item.Lng) * 1.3;
                    double travelMin = (distTo / speed) * 60;

                    if (b.CurrentTimeMin + travelMin + service <= b.MaxShiftMinutes)
                    {
                        b.CurrentKg += item.Weight;
                        b.CurrentM3 += item.Volume;
                        b.CurrentTimeMin += (travelMin + service);
                        b.LastLat = item.Lat;
                        b.LastLng = item.Lng;

                        b.Products.Add(new PreAssignProduct
                        {
                            ProductId = item.Post.ProductId.ToString(),
                            PostId = item.Post.PostId.ToString(),
                            UserName = item.UserName,
                            Address = item.FullAddress,
                            Weight = item.Weight,
                            Volume = item.Volume,
                            CategoryName = item.CategoryName,
                            BrandName = item.BrandName,
                            DimensionText = item.DimText
                        });
                        return true;
                    }
                }
            }
            return false;
        }

        private double CalculateHaversine(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371;
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
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

            double GetRawVal(string k)
            {
                if (!attMap.ContainsKey(k)) return 0;
                var pv = pValues.FirstOrDefault(v => v.AttributeId == attMap[k]);
                return pv?.Value ?? 0;
            }

            double rawLength = GetRawVal("Chiều dài");
            double rawWidth = GetRawVal("Chiều rộng");
            double rawHeight = GetRawVal("Chiều cao");

            // 3. CHUẨN HÓA VỀ MÉT (m) ĐỂ KHỚP VỚI KÍCH THƯỚC XE
            double lengthM = rawLength / 100.0;
            double widthM = rawWidth / 100.0;
            double heightM = rawHeight / 100.0;

            // 4. Tính toán thể tích (m3)
            double volumeM3 = 0;
            if (lengthM > 0 && widthM > 0 && heightM > 0)
            {
                // Tính toán trực tiếp trên đơn vị mét sẽ ra mét khối (m3)
                volumeM3 = lengthM * widthM * heightM;
            }
            else
            {
                // Logic dự phòng nếu thiếu 1 trong 3 chiều: Lấy từ EstimateVolume của Option
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
                            // Giả sử EstimateVolume trong DB đang lưu là m3, nếu là cm3 thì phải chia 1,000,000
                            volumeM3 = opt.EstimateVolume.Value;
                            break;
                        }
                    }
                }
            }

            // Giá trị thể tích tối thiểu (0.001 m3 tương đương 1 lít) tránh việc volume = 0 gây lỗi logic
            if (volumeM3 <= 0) volumeM3 = 0.001;

            // Trả về kết quả đã chuẩn hóa đơn vị MÉT
            return (weight, Math.Round(volumeM3, 5), lengthM, widthM, heightM);
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
                var days = System.Text.Json.JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>(raw, opts);

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
                var days = System.Text.Json.JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>(raw, opts);

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
    }
}
