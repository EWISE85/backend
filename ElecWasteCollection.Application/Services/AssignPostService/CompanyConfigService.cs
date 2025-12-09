using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;

namespace ElecWasteCollection.Application.Services.AssignPostService
{
    public class CompanyConfigService : ICompanyConfigService
    {
        public CompanyConfigResponse UpdateCompanyConfig(CompanyConfigRequest request)
        {
            if (request == null || request.Companies == null || !request.Companies.Any())
            {
                return new CompanyConfigResponse
                {
                    Message = "Danh sách company không được rỗng.",
                    Companies = new List<CompanyConfigDto>()
                };
            }

            foreach (var company in request.Companies)
            {
                if (company.SmallPoints == null || !company.SmallPoints.Any())
                {
                    return new CompanyConfigResponse
                    {
                        Message = $"Company {company.CompanyId} không có smallPoints.",
                        Companies = new List<CompanyConfigDto>()
                    };
                }

                foreach (var sp in company.SmallPoints)
                {
                    if (sp.RadiusKm <= 0)
                    {
                        return new CompanyConfigResponse
                        {
                            Message = $"SmallPoint {sp.SmallPointId} radiusKm không hợp lệ.",
                            Companies = new List<CompanyConfigDto>()
                        };
                    }

                    if (sp.MaxRoadDistanceKm <= 0)
                    {
                        return new CompanyConfigResponse
                        {
                            Message = $"SmallPoint {sp.SmallPointId} maxRoadDistanceKm không hợp lệ.",
                            Companies = new List<CompanyConfigDto>()
                        };
                    }
                }
            }

            double totalRatio = request.Companies.Sum(c => c.RatioPercent);

            if (Math.Abs(totalRatio - 100) > 0.0001)
            {
                return new CompanyConfigResponse
                {
                    Message = $"Tổng RatioPercent phải bằng 100%. Hiện tại = {totalRatio}%."
                };
            }

            FakeDataSeeder.CompanyConfigs = request.Companies;

            var companyDtos = request.Companies.Select(company =>
            {
                var realCompany = FakeDataSeeder.collectionTeams
                    .FirstOrDefault(t => t.CollectionCompanyId == company.CompanyId);

                var smallPointDtos = company.SmallPoints.Select(cfgSp =>
                {
                    var realSP = FakeDataSeeder.smallCollectionPoints
                        .FirstOrDefault(p => p.SmallCollectionPointsId == cfgSp.SmallPointId);

                    return new SmallPointDto
                    {
                        SmallPointId = cfgSp.SmallPointId,
                        Name = realSP?.Name,
                        Lat = realSP?.Latitude ?? 0,
                        Lng = realSP?.Longitude ?? 0,
                        RadiusKm = cfgSp.RadiusKm,
                        MaxRoadDistanceKm = cfgSp.MaxRoadDistanceKm,
                        Active = cfgSp.Active
                    };
                }).ToList();

                return new CompanyConfigDto
                {
                    CompanyId = company.CompanyId,
                    CompanyName = realCompany?.Name ?? $"Company {company.CompanyId}",
                    RatioPercent = company.RatioPercent,
                    SmallPoints = smallPointDtos
                };
            }).ToList();

            return new CompanyConfigResponse
            {
                Message = "Company configuration updated successfully.",
                Companies = companyDtos
            };
        }

        public CompanyConfigResponse GetCompanyConfig()
        {
            var companyDtos = FakeDataSeeder.CompanyConfigs.Select(company =>
            {
                var realCompany = FakeDataSeeder.collectionTeams
                    .FirstOrDefault(t => t.CollectionCompanyId == company.CompanyId);

                string companyName = realCompany?.Name ?? $"Company {company.CompanyId}";

                var smallPoints = company.SmallPoints.Select(cfgSp =>
                {
                    var realSP = FakeDataSeeder.smallCollectionPoints
                        .FirstOrDefault(p => p.SmallCollectionPointsId == cfgSp.SmallPointId);

                    return new SmallPointDto
                    {
                        SmallPointId = cfgSp.SmallPointId,
                        Name = realSP?.Name,
                        Lat = realSP?.Latitude ?? 0,
                        Lng = realSP?.Longitude ?? 0,
                        RadiusKm = cfgSp.RadiusKm,
                        MaxRoadDistanceKm = cfgSp.MaxRoadDistanceKm,
                        Active = cfgSp.Active
                    };
                }).ToList();

                return new CompanyConfigDto
                {
                    CompanyId = company.CompanyId,
                    CompanyName = companyName,
                    RatioPercent = company.RatioPercent,
                    SmallPoints = smallPoints
                };
            }).ToList();

            return new CompanyConfigResponse
            {
                Message = "Success",
                Companies = companyDtos
            };
        }
    }

}
