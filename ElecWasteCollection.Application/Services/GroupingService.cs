using System.Text.Json;
using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Application.Interfaces;
using ElecWasteCollection.Application.Model.GroupModel;
using ElecWasteCollection.Domain.Entities;
using static ElecWasteCollection.Application.Data.FakeDataSeeder;

namespace ElecWasteCollection.Application.Services
{
    public class GroupingService : IGroupingService
    {
        private const double SERVICE_TIME_MINUTES = 15; 
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

        private static bool TryParseScheduleInfo(string rawSchedule, out PostScheduleInfo info)
        {
            info = new PostScheduleInfo();
            if (string.IsNullOrWhiteSpace(rawSchedule)) return false;

            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var days = JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>(rawSchedule, opts);

                if (days == null || !days.Any()) return false;

                var validDates = new List<DateOnly>();
                foreach (var d in days)
                {
                    if (DateOnly.TryParse(d.PickUpDate, out var date) && d.Slots != null)
                    {
                        if (TimeOnly.TryParse(d.Slots.StartTime, out var s) &&
                            TimeOnly.TryParse(d.Slots.EndTime, out var e) && s < e)
                        {
                            validDates.Add(date);
                        }
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

        private static bool TryGetTimeWindowForDate(string rawSchedule, DateOnly targetDate, out TimeOnly start, out TimeOnly end)
        {
            start = TimeOnly.MinValue;
            end = TimeOnly.MaxValue;

            if (string.IsNullOrWhiteSpace(rawSchedule)) return false;

            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var days = JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>(rawSchedule, opts);

                var match = days?.FirstOrDefault(d => DateOnly.TryParse(d.PickUpDate, out var date) && date == targetDate);

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
            catch { return false; }
        }

        public async Task<PreAssignResponse> PreAssignAsync(PreAssignRequest request)
        {
            var point = FakeDataSeeder.smallCollectionPoints.FirstOrDefault(p => p.Id == request.CollectionPointId)
                ?? throw new Exception("Không tìm thấy trạm thu gom.");

            var rawPosts = FakeDataSeeder.posts.Where(p =>
            {
                var prod = FakeDataSeeder.products.FirstOrDefault(x => x.Id == p.ProductId);
                return prod != null && prod.Status == "Chờ gom nhóm";
            }).ToList();

            if (!rawPosts.Any()) throw new Exception("Không có bài đăng nào ở trạng thái 'Chờ gom nhóm'.");

            var pool = new List<dynamic>();
            foreach (var p in rawPosts)
            {
                if (TryParseScheduleInfo(p.ScheduleJson!, out var scheduleInfo))
                {
                    var user = FakeDataSeeder.users.FirstOrDefault(u => u.UserId == p.SenderId);
                    var prod = FakeDataSeeder.products.FirstOrDefault(pr => pr.Id == p.ProductId);
                    if (user == null || prod == null) continue;

                    var size = FakeDataSeeder.sizeTiers.FirstOrDefault(t => t.SizeTierId == prod.SizeTierId);

                    pool.Add(new
                    {
                        Post = p,
                        Schedule = scheduleInfo,
                        Weight = size?.EstimatedWeight ?? 10, 
                        Volume = size?.EstimatedVolume ?? 1,
                        UserName = user.Name,
                        Address = user.Address
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

            // Lọc bài cần lấy theo từng ngày
            // Duyệt qua từng ngày trong danh sách ngày rảnh
            // Với mỗi ngày, lọc các bài đăng có ngày rảnh trùng với ngày đó
            // Sau đó, chọn các bài đăng sao cho tổng trọng lượng không vượt quá ngưỡng .... % trọng tải xe
            // và ưu tiên các bài đăng có hạn cuối sớm nhất, sau đó là ngày đặt sớm nhất
            // chọn xe phù hợp với tổng trọng lượng đã chọn
            // Gán hàng vào ngày đó và loại bỏ khỏi pool
            foreach (var currentDate in distinctDates)
            {

                var candidates = pool
                    .Where(x => ((List<DateOnly>)x.Schedule.SpecificDates).Contains(currentDate))
                    .OrderBy(x => x.Schedule.MaxDate) 
                    .ThenBy(x => x.Schedule.MinDate)
                    .ToList();

                if (!candidates.Any()) continue;

                double totalWeight = candidates.Sum(x => (double)x.Weight);

                var suggestedVehicle = FakeDataSeeder.vehicles
                    .Where(v => v.Status == "active")
                    .OrderBy(v => v.Capacity_Kg)
                    .FirstOrDefault(v => totalWeight <= v.Capacity_Kg * (request.LoadThresholdPercent / 100.0))
                    ?? FakeDataSeeder.vehicles.OrderBy(v => v.Capacity_Kg).Last();

                double limitKg = suggestedVehicle.Capacity_Kg * (request.LoadThresholdPercent / 100.0);

                double currentLoad = 0;
                var selectedPosts = new List<PreAssignPost>();
                var assignedItems = new List<dynamic>();

                foreach (var item in candidates)
                {
                    if (currentLoad + item.Weight <= limitKg)
                    {
                        currentLoad += item.Weight;
                        assignedItems.Add(item);

                        selectedPosts.Add(new PreAssignPost
                        {
                            PostId = item.Post.Id,
                            UserName = item.UserName,
                            Address = item.Address,
                            Weight = item.Weight,
                            Volume = item.Volume
                        });
                    }
                }

                foreach (var item in assignedItems)
                {
                    pool.Remove(item);
                }

                if (selectedPosts.Any())
                {
                    response.Days.Add(new PreAssignDay
                    {
                        WorkDate = currentDate,
                        OriginalPostCount = selectedPosts.Count,
                        TotalWeight = currentLoad,
                        TotalVolume = selectedPosts.Sum(x => (double)x.Volume),
                        SuggestedVehicle = new SuggestedVehicle
                        {
                            Id = suggestedVehicle.Id,
                            Plate_Number = suggestedVehicle.Plate_Number,
                            Vehicle_Type = suggestedVehicle.Vehicle_Type,
                            Capacity_Kg = suggestedVehicle.Capacity_Kg,
                            AllowedCapacityKg = limitKg
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
                throw new Exception("Trạm thu gom không tồn tại.");

            if (request.PostIds == null || !request.PostIds.Any())
                throw new Exception("Danh sách bài đăng không được để trống.");


            bool isVehicleBusy = FakeDataSeeder.stagingAssignDays.Any(s =>
                s.Date == request.WorkDate &&
                s.VehicleId == request.VehicleId &&
                s.PointId != request.CollectionPointId); 

            if (isVehicleBusy)
                throw new Exception($"Xe {vehicle.Plate_Number} đã được điều động cho trạm khác vào ngày {request.WorkDate}.");

            FakeDataSeeder.stagingAssignDays.RemoveAll(s =>
                s.Date == request.WorkDate &&
                s.PointId == request.CollectionPointId
            );

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
            var point = FakeDataSeeder.smallCollectionPoints.FirstOrDefault(p => p.Id == request.CollectionPointId)
                ?? throw new Exception("Không tìm thấy trạm thu gom.");

            var staging = FakeDataSeeder.stagingAssignDays
                .Where(s => s.PointId == request.CollectionPointId)
                .OrderBy(s => s.Date)
                .ToList();

            if (!staging.Any()) throw new Exception("Chưa có ngày nào được Assign cho trạm này.");

            var response = new GroupingByPointResponse
            {
                CollectionPoint = point.Name,
                SavedToDatabase = request.SaveResult
            };

            int groupCounter = 1;

            foreach (var assignDay in staging)
            {
                var workDate = assignDay.Date;
                var posts = FakeDataSeeder.posts.Where(p => assignDay.PostIds.Contains(p.Id)).ToList();
                if (!posts.Any()) continue; 

                var shiftsToday = FakeDataSeeder.shifts
                    .Where(s => s.WorkDate == workDate)
                    .OrderBy(s => s.Shift_Start_Time)
                    .ToList();

                if (!shiftsToday.Any())
                    throw new Exception($"Không tìm thấy ca làm việc (Shifts) cho ngày {workDate}. Vui lòng tạo ca trước.");

                var vehicle = FakeDataSeeder.vehicles.FirstOrDefault(v => v.Id == assignDay.VehicleId)
                    ?? throw new Exception($"Xe {assignDay.VehicleId} không tồn tại.");

                var nodes = new List<dynamic>();
                foreach (var p in posts)
                {
                    var user = FakeDataSeeder.users.FirstOrDefault(u => u.UserId == p.SenderId);
                    var prod = FakeDataSeeder.products.FirstOrDefault(pr => pr.Id == p.ProductId);
                    if (user == null || prod == null) continue;

                    var size = FakeDataSeeder.sizeTiers.FirstOrDefault(t => t.SizeTierId == prod.SizeTierId);

                    if (TryGetTimeWindowForDate(p.ScheduleJson!, workDate, out var start, out var end))
                    {
                        nodes.Add(new
                        {
                            Post = p,
                            User = user,
                            Start = start,
                            End = end,
                            Weight = size?.EstimatedWeight ?? 10,
                            Volume = size?.EstimatedVolume ?? 1,
                            SizeName = size?.Name ?? "Standard",
                            Lat = user.Iat ?? point.Latitude,
                            Lng = user.Ing ?? point.Longitude
                        });
                    }
                }

                var unassignedNodes = new List<dynamic>(nodes);

                foreach (var shift in shiftsToday)
                {
                    if (!unassignedNodes.Any()) break;

                    var collector = FakeDataSeeder.collectors.FirstOrDefault(c => c.CollectorId == shift.CollectorId);
                    var shiftStart = TimeOnly.FromDateTime(shift.Shift_Start_Time);
                    var shiftEnd = TimeOnly.FromDateTime(shift.Shift_End_Time);

                    double curKg = 0;
                    double curM3 = 0;
                    double curLat = point.Latitude;
                    double curLng = point.Longitude;
                    TimeOnly timeCursor = shiftStart;

                    var routeNodes = new List<RouteDetail>();
                    var routeDbList = new List<CollectionRoutes>();

                    while (unassignedNodes.Any())
                    {
                        double speed = (vehicle.Vehicle_Type.Contains("lớn") ? SPEED_KM_H_LARGE : SPEED_KM_H_SMALL) / 60.0; 

                        var bestCandidate = unassignedNodes
                            .Select(node =>
                            {
                                double dist = GeoHelper.DistanceKm(curLat, curLng, node.Lat, node.Lng);
                                double travelTime = dist / speed;
                                TimeOnly arrivalTime = timeCursor.AddMinutes(travelTime);

                                double waitTime = 0;
                                if (arrivalTime < node.Start)
                                {
                                    waitTime = (node.Start - arrivalTime).TotalMinutes;
                                }

                                TimeOnly actualStart = arrivalTime < node.Start ? node.Start : arrivalTime;

                                bool isValid = true;
                                if (curKg + node.Weight > vehicle.Capacity_Kg) isValid = false;
                                if (curM3 + node.Volume > vehicle.Capacity_M3) isValid = false;

                                if (actualStart > node.End) isValid = false;

                                if (actualStart.AddMinutes(SERVICE_TIME_MINUTES) > shiftEnd) isValid = false;

                                return new
                                {
                                    Data = node,
                                    Distance = dist,
                                    WaitTime = waitTime,
                                    ActualStart = actualStart,
                                    IsValid = isValid,
                                    ClosingTime = node.End
                                };
                            })
                            .Where(x => x.IsValid)
                            .OrderBy(x => x.WaitTime)     
                            .ThenBy(x => x.ClosingTime)  
                            .ThenBy(x => x.Distance)      
                            .FirstOrDefault();

                        if (bestCandidate == null) break; 

                        var chosen = bestCandidate.Data;

                        routeNodes.Add(new RouteDetail
                        {
                            PickupOrder = routeNodes.Count + 1,
                            PostId = chosen.Post.Id,
                            UserName = chosen.User.Name,
                            Address = chosen.User.Address,
                            DistanceKm = Math.Round(bestCandidate.Distance, 2),
                            Schedule = chosen.Post.ScheduleJson,
                            EstimatedArrival = bestCandidate.ActualStart.ToString("HH:mm"),
                            WeightKg = chosen.Weight,
                            VolumeM3 = chosen.Volume,
                            SizeTier = chosen.SizeName
                        });

                        routeDbList.Add(new CollectionRoutes
                        {
                            CollectionRouteId = Guid.NewGuid(),
                            PostId = chosen.Post.Id,
                            CollectionDate = workDate,
                            EstimatedTime = bestCandidate.ActualStart,
                            Status = "Chưa bắt đầu"
                        });

                        curKg += chosen.Weight;
                        curM3 += chosen.Volume;
                        curLat = chosen.Lat;
                        curLng = chosen.Lng;
                        timeCursor = bestCandidate.ActualStart.AddMinutes(SERVICE_TIME_MINUTES);

                        unassignedNodes.Remove(chosen);

                        var prodUpdate = FakeDataSeeder.products.FirstOrDefault(p => p.Id == chosen.Post.ProductId);
                        if (prodUpdate != null) prodUpdate.Status = "Chờ thu gom";
                    }

                    if (routeNodes.Any())
                    {
                        var group = new CollectionGroups
                        {
                            Id = FakeDataSeeder.collectionGroups.Count + 1,
                            Group_Code = $"GRP-{workDate:MMdd}-{groupCounter++}",
                            Name = $"{vehicle.Vehicle_Type} - {vehicle.Plate_Number}",
                            Shift_Id = shift.Id,
                            Created_At = DateTime.Now
                        };

                        foreach (var rt in routeDbList) rt.CollectionGroupId = group.Id;

                        if (request.SaveResult)
                        {
                            FakeDataSeeder.collectionGroups.Add(group);
                            FakeDataSeeder.collectionRoutes.AddRange(routeDbList);
                        }

                        response.CreatedGroups.Add(new GroupSummary
                        {
                            GroupCode = group.Group_Code,
                            ShiftId = shift.Id,
                            Vehicle = $"{vehicle.Plate_Number} ({vehicle.Vehicle_Type})",
                            Collector = collector?.Name ?? "Unknown",
                            GroupDate = workDate,
                            TotalPosts = routeNodes.Count,
                            TotalWeightKg = curKg,
                            TotalVolumeM3 = curM3,
                            Routes = routeNodes
                        });
                    }
                }

                //Nếu k còn ca nào nữa mà vẫn còn bài chưa được gán, thì báo lỗi
                if (unassignedNodes.Any())
                {

                }

            } 

            return await Task.FromResult(response);
        }
    }
}