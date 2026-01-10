using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services.AssignPostService
{
    public class CompanyConfigService : ICompanyConfigService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompanyConfigService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CompanyConfigResponse> UpdateCompanyConfigAsync(CompanyConfigRequest request)
        {
            if (request == null || request.Companies == null || !request.Companies.Any())
            {
                return new CompanyConfigResponse
                {
                    Message = "Danh sách company không được rỗng.",
                    Companies = new List<CompanyConfigDto>()
                };
            }

            double totalRatio = request.Companies.Sum(c => c.RatioPercent);
            if (Math.Abs(totalRatio - 100) > 0.0001)
            {
                return new CompanyConfigResponse
                {
                    Message = $"Tổng RatioPercent phải bằng 100%. Hiện tại = {totalRatio}%."
                };
            }

            var updatedDtos = new List<CompanyConfigDto>();

            try
            {
                var companyRepo = _unitOfWork.Companies;
                var configRepo = _unitOfWork.SystemConfig;

                var allCompanies = await companyRepo.GetAllAsync(includeProperties: "SmallCollectionPoints");

                var allConfigs = await configRepo.GetAllAsync();

                foreach (var companyDto in request.Companies)
                {
                    if (companyDto.SmallPoints == null || !companyDto.SmallPoints.Any())
                    {
                        return new CompanyConfigResponse
                        {
                            Message = $"Company {companyDto.CompanyId} không có smallPoints.",
                            Companies = new List<CompanyConfigDto>()
                        };
                    }

                    var companyEntity = allCompanies.FirstOrDefault(c => c.CompanyId == companyDto.CompanyId);

                    if (companyEntity != null)
                    {
                        await UpsertConfigAsync(allConfigs, companyEntity.CompanyId, null, SystemConfigKey.ASSIGN_RATIO, companyDto.RatioPercent.ToString());

                        companyEntity.Updated_At = DateTime.UtcNow; 

                        var spDtos = new List<SmallPointDto>();

                        foreach (var spDto in companyDto.SmallPoints)
                        {
                            if (spDto.RadiusKm <= 0) return ErrorResponse($"SmallPoint {spDto.SmallPointId} radiusKm không hợp lệ.");
                            if (spDto.MaxRoadDistanceKm <= 0) return ErrorResponse($"SmallPoint {spDto.SmallPointId} maxRoadDistanceKm không hợp lệ.");

                            var spEntity = companyEntity.SmallCollectionPoints
                                .FirstOrDefault(p => p.SmallCollectionPointsId == spDto.SmallPointId);

                            if (spEntity != null)
                            {
                                await UpsertConfigAsync(allConfigs, null, spEntity.SmallCollectionPointsId, SystemConfigKey.RADIUS_KM, spDto.RadiusKm.ToString());
                                await UpsertConfigAsync(allConfigs, null, spEntity.SmallCollectionPointsId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, spDto.MaxRoadDistanceKm.ToString());

                                spDtos.Add(new SmallPointDto
                                {
                                    SmallPointId = spEntity.SmallCollectionPointsId,
                                    Name = spEntity.Name,
                                    Lat = spEntity.Latitude,
                                    Lng = spEntity.Longitude,
                                    RadiusKm = spDto.RadiusKm, 
                                    MaxRoadDistanceKm = spDto.MaxRoadDistanceKm,
                                    Active = true
                                });
                            }
                        }

                        companyRepo.Update(companyEntity);

                        updatedDtos.Add(new CompanyConfigDto
                        {
                            CompanyId = companyEntity.CompanyId,
                            CompanyName = companyEntity.Name,
                            RatioPercent = companyDto.RatioPercent,
                            SmallPoints = spDtos
                        });
                    }
                }

                await _unitOfWork.SaveAsync();

                return new CompanyConfigResponse
                {
                    Message = "Company configuration updated successfully.",
                    Companies = updatedDtos
                };
            }
            catch (Exception ex)
            {
                return new CompanyConfigResponse
                {
                    Message = $"Lỗi hệ thống: {ex.Message}",
                    Companies = new List<CompanyConfigDto>()
                };
            }
        }

        public async Task<CompanyConfigResponse> GetCompanyConfigAsync()
        {
            try
            {
                var companyRepo = _unitOfWork.Companies;
                var configRepo = _unitOfWork.SystemConfig;

                var companies = await companyRepo.GetAllAsync(
                    filter: c => c.CompanyType == CompanyType.CollectionCompany.ToString(),
                    includeProperties: "SmallCollectionPoints"
                );

                var allConfigs = await configRepo.GetAllAsync();

                var companyDtos = companies.Select(c => new CompanyConfigDto
                {
                    CompanyId = c.CompanyId,
                    CompanyName = c.Name,
                    RatioPercent = GetConfigValue(allConfigs, c.CompanyId, null, SystemConfigKey.ASSIGN_RATIO, 0),

                    SmallPoints = c.SmallCollectionPoints.Select(sp => new SmallPointDto
                    {
                        SmallPointId = sp.SmallCollectionPointsId,
                        Name = sp.Name,
                        Lat = sp.Latitude,
                        Lng = sp.Longitude,
                        RadiusKm = GetConfigValue(allConfigs, null, sp.SmallCollectionPointsId, SystemConfigKey.RADIUS_KM, 0),
                        MaxRoadDistanceKm = GetConfigValue(allConfigs, null, sp.SmallCollectionPointsId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, 0),
                        Active = true
                    }).ToList()
                }).ToList();

                return new CompanyConfigResponse
                {
                    Message = "Success",
                    Companies = companyDtos
                };
            }
            catch (Exception ex)
            {
                return new CompanyConfigResponse { Message = ex.Message };
            }
        }


        private async Task UpsertConfigAsync(IEnumerable<SystemConfig> allConfigs, string? companyId, string? pointId, SystemConfigKey key, string value)
        {
            var configRepo = _unitOfWork.SystemConfig;

            var config = allConfigs.FirstOrDefault(x =>
                x.Key == key.ToString() &&
                x.CompanyId == companyId &&
                x.SmallCollectionPointId == pointId);

            if (config != null)
            {
                config.Value = value;
                configRepo.Update(config);
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
                    Status = SystemConfigStatus.Active.ToString(),
                    DisplayName = key.ToString(),
                    GroupName = companyId != null ? "CompanyConfig" : "PointConfig"
                };
                await configRepo.AddAsync(newConfig);
            }
        }

        private double GetConfigValue(IEnumerable<SystemConfig> configs, string? companyId, string? pointId, SystemConfigKey key, double defaultValue)
        {
            var config = configs.FirstOrDefault(x =>
                x.Key == key.ToString() &&
                x.CompanyId == companyId &&
                x.SmallCollectionPointId == pointId);

            if (config != null && double.TryParse(config.Value, out double result))
            {
                return result;
            }
            return defaultValue;
        }

        private CompanyConfigResponse ErrorResponse(string msg)
        {
            return new CompanyConfigResponse
            {
                Message = msg,
                Companies = new List<CompanyConfigDto>()
            };
        }
    }
}