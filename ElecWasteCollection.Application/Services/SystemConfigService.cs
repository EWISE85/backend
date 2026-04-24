using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.AssignPost;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.AspNetCore.Http;

namespace ElecWasteCollection.Application.Services
{
    public class SystemConfigService : ISystemConfigService
    {
        private readonly ISystemConfigRepository _systemConfigRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ICollectionUnitRepository _smallCollectionRepository;
        private readonly ICompanyRepository _companyRepository;

        public SystemConfigService(ISystemConfigRepository systemConfigRepository, IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService, ICollectionUnitRepository smallCollectionRepository, ICompanyRepository companyRepository)
        {
            _systemConfigRepository = systemConfigRepository;
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _companyRepository = companyRepository;
            _smallCollectionRepository = smallCollectionRepository;
        }

        public async Task<bool> CreateNewConfigWithFileAsync(IFormFile file)
        {
            string fileUrl = await _cloudinaryService.UploadRawFileAsync(file, SystemConfigKey.FORMAT_IMPORT_VEHICLE.ToString());

            var newConfig = new SystemConfig
            {
                SystemConfigId = Guid.NewGuid(),
                Key = SystemConfigKey.FORMAT_IMPORT_VEHICLE.ToString(),
                Value = fileUrl,
                DisplayName = "Mẫu phương tiện excel",
                GroupName = "Excel",
                Status = SystemConfigStatus.DANG_HOAT_DONG.ToString()
            };

            await _unitOfWork.SystemConfig.AddAsync(newConfig);
            await _unitOfWork.SaveAsync();
            return true;
        }

        public async Task<(byte[] fileBytes, string fileName)> DownloadFileByConfigIdAsync(Guid id)
        {
            var config = await _systemConfigRepository.GetByIdAsync(id);
            if (config == null || string.IsNullOrEmpty(config.Value))
            {
                throw new Exception("Không tìm thấy cấu hình hoặc URL file.");
            }

            using var httpClient = new HttpClient();
            var fileBytes = await httpClient.GetByteArrayAsync(config.Value);

            string fileName = Path.GetFileName(config.Value) ?? "downloaded_file.xlsx";

            return (fileBytes, fileName);
        }
        public async Task<List<SystemConfigModel>> GetAllSystemConfigActive(string? groupName, string? companyId, string? scpId)
        {
            var activeConfigs = await _systemConfigRepository.GetActiveConfigsByFilterAsync(groupName, companyId, scpId);

            if (activeConfigs == null || !activeConfigs.Any()) return new List<SystemConfigModel>();

            var result = new List<SystemConfigModel>();

            foreach (var config in activeConfigs)
            {
                string cName = "N/A";
                if (!string.IsNullOrEmpty(config.CompanyId))
                {
                    cName = await _companyRepository.GetCompanyNameAsync(config.CompanyId) ?? "Không tìm thấy Công ty";
                }

                string sName = "N/A";
                if (!string.IsNullOrEmpty(config.CollectionUnitId))
                {
                    sName = await _smallCollectionRepository.GetScpNameAsync(config.CollectionUnitId) ?? "Không tìm thấy SCP";
                }

                result.Add(new SystemConfigModel
                {
                    SystemConfigId = config.SystemConfigId,
                    Key = config.Key,
                    Value = config.Value,
                    DisplayName = config.DisplayName,
                    GroupName = config.GroupName,
                    Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<SystemConfigStatus>(config.Status),
                    CompanyName = cName,
                    ScpName = sName
                });
            }

            return result;
        }

        public async Task<SystemConfigModel> GetSystemConfigByKey(string key)
        {
            var config = await _systemConfigRepository
                .GetAsync(c => c.Key.ToLower() == key.ToLower()
                               && c.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString());

            if (config == null) throw new AppException("không tìm thấy config", 404);

            return new SystemConfigModel
            {
                SystemConfigId = config.SystemConfigId,
                Key = config.Key,
                Value = config.Value,
                DisplayName = config.DisplayName,
                GroupName = config.GroupName
            };
        }

        public async Task<bool> UpdateSystemConfig(UpdateSystemConfigModel model)
        {
            var config = await _systemConfigRepository
                .GetAsync(c => c.SystemConfigId == model.SystemConfigId);

            if (config == null) throw new AppException("không tìm thấy config", 404);

            if (!string.IsNullOrEmpty(model.Value))
            {
                config.Value = model.Value;
            }
            else if (model.ExcelFile != null)
            {
                var value = await _cloudinaryService.UploadRawFileAsync(model.ExcelFile, config.Key);
                config.Value = value;
            }
            _unitOfWork.SystemConfig.Update(config);
            await _unitOfWork.SaveAsync();
            return true;
        }

        public async Task<PagedResult<WarehouseSpeedResponse>> GetWarehouseSpeedsPagedAsync(int page, int limit, string? searchTerm)
        {
            var allPoints = await _unitOfWork.CollectionUnits.GetAllAsync();
            var speedConfigs = await _systemConfigRepository.GetsAsync(c =>
                c.Key == SystemConfigKey.TRANSPORT_SPEED.ToString());

            var query = allPoints.Select(point =>
            {
                var config = speedConfigs.FirstOrDefault(c => c.CollectionUnitId == point.CollectionUnitId);

                return new WarehouseSpeedResponse
                {
                    SystemConfigId = config?.SystemConfigId ?? Guid.Empty,
                    SmallCollectionPointId = point.CollectionUnitId,
                    DisplayName = point.Name,
                    Value = config?.Value ?? "0",

                    Status = config != null ? (config.Status ?? "Chưa cấu hình") : "Chưa cấu hình",

                    Key = config?.Key ?? SystemConfigKey.TRANSPORT_SPEED.ToString(),
                    GroupName = config?.GroupName ?? "PointConfig"
                };
            }).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(c =>
                    (c.DisplayName != null && c.DisplayName.ToLower().Contains(searchTerm)) ||
                    (c.SmallCollectionPointId != null && c.SmallCollectionPointId.ToLower().Contains(searchTerm))
                );
            }

            var totalItems = query.Count();
            var pagedData = query.Skip((page - 1) * limit).Take(limit).ToList();

            return new PagedResult<WarehouseSpeedResponse>
            {
                Page = page,
                Limit = limit,
                TotalItems = totalItems,
                Data = pagedData
            };
        }

        public async Task<bool> UpsertWarehouseSpeedAsync(WarehouseSpeedRequest model)
        {
            var config = await _systemConfigRepository.GetAsync(c =>
                c.Key == SystemConfigKey.TRANSPORT_SPEED.ToString() &&
                c.CollectionUnitId == model.SmallCollectionPointId);

            if (config != null)
            {
                config.Value = model.SpeedKmh.ToString();
                config.Status = SystemConfigStatus.DANG_HOAT_DONG.ToString();
                config.DisplayName = SystemConfigKey.TRANSPORT_SPEED.ToString();
                config.GroupName = "PointConfig";

                _unitOfWork.SystemConfig.Update(config);
            }
            else
            {
                var newConfig = new SystemConfig
                {
                    SystemConfigId = Guid.NewGuid(),
                    Key = SystemConfigKey.TRANSPORT_SPEED.ToString(),
                    Value = model.SpeedKmh.ToString(),
                    DisplayName = SystemConfigKey.TRANSPORT_SPEED.ToString(),
                    GroupName = "PointConfig",
                    Status = SystemConfigStatus.DANG_HOAT_DONG.ToString(),
                    CollectionUnitId = model.SmallCollectionPointId
                };
                await _unitOfWork.SystemConfig.AddAsync(newConfig);
            }

            return await _unitOfWork.SaveAsync() > 0;
        }
        public async Task<bool> UpdateWarehouseSpeedAsync(WarehouseSpeedRequest model)
        {
            var config = await _systemConfigRepository.GetAsync(c =>
                c.Key == SystemConfigKey.TRANSPORT_SPEED.ToString() &&
                c.CollectionUnitId == model.SmallCollectionPointId &&
                c.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString());

            if (config == null)
            {
                throw new AppException($"Không tìm thấy cấu hình tốc độ đang hoạt động cho kho {model.SmallCollectionPointId}", 404);
            }

            config.Value = model.SpeedKmh.ToString();

            config.DisplayName = SystemConfigKey.TRANSPORT_SPEED.ToString();
            config.GroupName = "PointConfig";

            _unitOfWork.SystemConfig.Update(config);
            return await _unitOfWork.SaveAsync() > 0;
        }

        public async Task<bool> DeleteWarehouseSpeedAsync(string smallCollectionPointId)
        {
            var config = await _systemConfigRepository.GetAsync(c =>
                c.Key == SystemConfigKey.TRANSPORT_SPEED.ToString() &&
                c.CollectionUnitId == smallCollectionPointId &&
                c.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString());

            if (config == null)
            {
                throw new AppException("Không tìm thấy cấu hình tốc độ cho kho này hoặc đã bị xóa trước đó", 404);
            }

            config.Status = SystemConfigStatus.KHONG_HOAT_DONG.ToString();

            _unitOfWork.SystemConfig.Update(config);
            return await _unitOfWork.SaveAsync() > 0;
        }

        public async Task<WarehouseSpeedResponse> GetWarehouseSpeedByPointIdAsync(string smallPointId)
        {
            if (string.IsNullOrWhiteSpace(smallPointId))
                throw new ArgumentException("ID điểm thu gom không được để trống.");

            var point = await _unitOfWork.CollectionUnits.GetByIdAsync(smallPointId);
            if (point == null)
                throw new KeyNotFoundException($"Không tìm thấy điểm thu gom với ID: {smallPointId}");

            var config = await _systemConfigRepository.GetAsync(c =>
                c.Key == SystemConfigKey.TRANSPORT_SPEED.ToString() &&
                c.CollectionUnitId == smallPointId);

            return new WarehouseSpeedResponse
            {
                SystemConfigId = config?.SystemConfigId ?? Guid.Empty,
                SmallCollectionPointId = point.CollectionUnitId,
                DisplayName = point.Name,
                Value = config?.Value ?? "0",

                Status = config != null ? (config.Status ?? "Chưa cấu hình") : "Chưa cấu hình",

                Key = config?.Key ?? SystemConfigKey.TRANSPORT_SPEED.ToString(),
                GroupName = config?.GroupName ?? "PointConfig"
            };
        }

        //Tự động chia
        public async Task<AutoAssignSettings> GetAutoAssignSettingsAsync()
        {
            var configs = await _systemConfigRepository.GetsAsync(c =>
                c.GroupName == "AutoAssign" && c.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString());

            return new AutoAssignSettings
            {
                IsEnabled = configs.FirstOrDefault(c => c.Key == SystemConfigKey.AUTO_ASSIGN_ENABLED.ToString())?.Value.ToLower() == "true",
                ImmediateThreshold = int.Parse(configs.FirstOrDefault(c => c.Key == SystemConfigKey.AUTO_ASSIGN_IMMEDIATE_THRESHOLD.ToString())?.Value ?? "200"),
                ScheduleTime = configs.FirstOrDefault(c => c.Key == SystemConfigKey.AUTO_ASSIGN_SCHEDULE_TIME.ToString())?.Value ?? "23:59",
                ScheduleMinQty = int.Parse(configs.FirstOrDefault(c => c.Key == SystemConfigKey.AUTO_ASSIGN_SCHEDULE_MIN_QTY.ToString())?.Value ?? "100")
            };
        }

        public async Task<bool> UpdateAutoAssignSettingsAsync(UpdateAutoAssignRequest model)
        {
            var keysToUpdate = new Dictionary<SystemConfigKey, string?>();

            if (model.IsEnabled.HasValue) keysToUpdate.Add(SystemConfigKey.AUTO_ASSIGN_ENABLED, model.IsEnabled.Value.ToString().ToLower());
            if (model.ImmediateThreshold.HasValue) keysToUpdate.Add(SystemConfigKey.AUTO_ASSIGN_IMMEDIATE_THRESHOLD, model.ImmediateThreshold.Value.ToString());
            if (!string.IsNullOrEmpty(model.ScheduleTime)) keysToUpdate.Add(SystemConfigKey.AUTO_ASSIGN_SCHEDULE_TIME, model.ScheduleTime);
            if (model.ScheduleMinQty.HasValue) keysToUpdate.Add(SystemConfigKey.AUTO_ASSIGN_SCHEDULE_MIN_QTY, model.ScheduleMinQty.Value.ToString());

            foreach (var item in keysToUpdate)
            {
                var config = await _systemConfigRepository.GetAsync(c => c.Key == item.Key.ToString());
                if (config != null)
                {
                    config.Value = item.Value!;
                    _unitOfWork.SystemConfig.Update(config);
                }
                else
                {
                    await _unitOfWork.SystemConfig.AddAsync(new SystemConfig
                    {
                        SystemConfigId = Guid.NewGuid(),
                        Key = item.Key.ToString(),
                        Value = item.Value!,
                        DisplayName = item.Key.ToString(),
                        GroupName = "AutoAssign",
                        Status = SystemConfigStatus.DANG_HOAT_DONG.ToString()
                    });
                }
            }

            return await _unitOfWork.SaveAsync() > 0;
        }

        public async Task<WarehouseLoadThresholdSettings> GetWarehouseLoadThresholdAsync()
        {
            // Tìm cấu hình dựa trên Key
            var config = await _systemConfigRepository.GetAsync(c =>
                c.Key == SystemConfigKey.WAREHOUSE_LOAD_THRESHOLD.ToString() &&
                c.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString());

            return new WarehouseLoadThresholdSettings
            {
                // Nếu config null, trả về 0.7 làm mặc định
                Threshold = double.TryParse(config?.Value, out var val) ? val : 0.7,
                SystemConfigId = config?.SystemConfigId ?? Guid.Empty,
                DisplayName = config?.DisplayName ?? "Ngưỡng tải trọng kho hàng (Load Balancing)",
                Status = config?.Status ?? "Chưa cấu hình"
            };

        }

        public async Task<bool> UpdateWarehouseLoadThresholdAsync(UpdateThresholdRequest model)
        {
            // Chuẩn hóa giá trị (Ví dụ nhập 70 thì hiểu là 0.7)
            double standardizedValue = model.Threshold > 1 ? model.Threshold / 100 : model.Threshold;

            if (standardizedValue < 0 || standardizedValue > 1)
            {
                throw new AppException("Giá trị ngưỡng không hợp lệ (Ví dụ: 70 cho 70% hoặc 0.7)", 400);
            }

            // Tìm cấu hình tốc độ dựa trên Key và ID kho cụ thể
            var key = SystemConfigKey.WAREHOUSE_LOAD_THRESHOLD.ToString();
            var config = await _systemConfigRepository.GetAsync(c =>
                c.Key == key &&
                c.CollectionUnitId == model.SmallCollectionPointId);

            if (config != null)
            {
                // Cập nhật cấu hình hiện có cho kho này
                config.Value = standardizedValue.ToString();
                config.Status = SystemConfigStatus.DANG_HOAT_DONG.ToString();
                config.DisplayName = "Ngưỡng tải trọng kho hàng";
                config.GroupName = "LoadThreshold";

                _unitOfWork.SystemConfig.Update(config);
            }
            else
            {
                // Tạo mới cấu hình riêng cho kho này nếu chưa tồn tại
                var newConfig = new SystemConfig
                {
                    SystemConfigId = Guid.NewGuid(),
                    Key = key,
                    Value = standardizedValue.ToString(),
                    DisplayName = "Ngưỡng tải trọng kho hàng",
                    GroupName = "LoadThreshold",
                    Status = SystemConfigStatus.DANG_HOAT_DONG.ToString(),
                    CollectionUnitId = model.SmallCollectionPointId
                };
                await _unitOfWork.SystemConfig.AddAsync(newConfig);
            }

            // Lưu vào database thông qua UnitOfWork
            return await _unitOfWork.SaveAsync() > 0;
        }

        public async Task<double> GetImageSimilarityThresholdAsync()
        {
            var config = await _unitOfWork.SystemConfig.GetAsync(c => c.Key.ToLower() == SystemConfigKey.IMAGE_SIMILARITY_THRESHOLD.ToString().ToLower());
            if (config == null)
            {
                throw new AppException("Không tìm thấy cấu hình ngưỡng tương đồng hình ảnh", 404);
            }
            return double.TryParse(config.Value, out var threshold) ? threshold : 0.8;
        }

        public async Task<AutoGroupingSettingResponse> GetAutoGroupingSettingAsync(string scpId)
        {
            var configs = await _systemConfigRepository.GetsAsync(c =>
                c.CollectionUnitId == scpId && c.GroupName == "AutoGrouping");

            var point = await _unitOfWork.CollectionUnits.GetByIdAsync(scpId);

            return new AutoGroupingSettingResponse
            {
                CollectionUnitId = scpId,
                CollectionUnitName = point?.Name ?? "N/A",
                IsEnabled = configs.FirstOrDefault(c => c.Key == SystemConfigKey.AUTO_GROUP_ENABLED.ToString())?.Value.ToLower() == "true",
                ScheduleTime = configs.FirstOrDefault(c => c.Key == SystemConfigKey.AUTO_GROUP_TIME.ToString())?.Value ?? "07:00"
            };
        }

        public async Task<bool> UpdateAutoGroupingSettingAsync(AutoGroupingSettingModel model)
        {
            var keysToUpdate = new Dictionary<string, string>
    {
        { SystemConfigKey.AUTO_GROUP_ENABLED.ToString(), model.IsEnabled.ToString().ToLower() },
        { SystemConfigKey.AUTO_GROUP_TIME.ToString(), model.ScheduleTime },
        { SystemConfigKey.AUTO_GROUP_LOAD_THRESHOLD.ToString(), model.LoadThresholdPercent.ToString() }
    };

            foreach (var item in keysToUpdate)
            {
                var config = await _unitOfWork.SystemConfig.GetAsync(c =>
                    c.Key == item.Key && c.CollectionUnitId == model.CollectionUnitId);

                if (config != null)
                {
                    config.Value = item.Value;
                    _unitOfWork.SystemConfig.Update(config);
                }
                else
                {
                    await _unitOfWork.SystemConfig.AddAsync(new SystemConfig
                    {
                        SystemConfigId = Guid.NewGuid(),
                        Key = item.Key,
                        Value = item.Value,
                        DisplayName = item.Key,
                        GroupName = "AutoGrouping",
                        Status = SystemConfigStatus.DANG_HOAT_DONG.ToString(),
                        CollectionUnitId = model.CollectionUnitId
                    });
                }
            }
            return await _unitOfWork.SaveAsync() > 0;
        }
    }
}
