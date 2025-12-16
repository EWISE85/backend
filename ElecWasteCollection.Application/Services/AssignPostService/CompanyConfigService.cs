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
                var companyRepo = _unitOfWork.CollectionCompanies;
                var allCompanies = await companyRepo.GetAllAsync(includeProperties: "SmallCollectionPoints");

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

                    var companyEntity = allCompanies.FirstOrDefault(c => c.CollectionCompanyId == companyDto.CompanyId);

                    if (companyEntity != null)
                    {
                        companyEntity.AssignRatio = companyDto.RatioPercent;
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
                                spEntity.RadiusKm = spDto.RadiusKm;
                                spEntity.MaxRoadDistanceKm = spDto.MaxRoadDistanceKm;

                                spDtos.Add(new SmallPointDto
                                {
                                    SmallPointId = spEntity.SmallCollectionPointsId,
                                    Name = spEntity.Name,
                                    Lat = spEntity.Latitude,
                                    Lng = spEntity.Longitude,
                                    RadiusKm = spEntity.RadiusKm,
                                    MaxRoadDistanceKm = spEntity.MaxRoadDistanceKm,
                                    Active = true
                                });
                            }
                        }

                        companyRepo.Update(companyEntity);

                        updatedDtos.Add(new CompanyConfigDto
                        {
                            CompanyId = companyEntity.CollectionCompanyId,
                            CompanyName = companyEntity.Name,
                            RatioPercent = companyEntity.AssignRatio,
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
                var companyRepo = _unitOfWork.CollectionCompanies;
                var companies = await companyRepo.GetAllAsync(includeProperties: "SmallCollectionPoints");

                var companyDtos = companies.Select(c => new CompanyConfigDto
                {
                    CompanyId = c.CollectionCompanyId,
                    CompanyName = c.Name,
                    RatioPercent = c.AssignRatio,
                    SmallPoints = c.SmallCollectionPoints.Select(sp => new SmallPointDto
                    {
                        SmallPointId = sp.SmallCollectionPointsId,
                        Name = sp.Name,
                        Lat = sp.Latitude,
                        Lng = sp.Longitude,
                        RadiusKm = sp.RadiusKm,
                        MaxRoadDistanceKm = sp.MaxRoadDistanceKm,
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