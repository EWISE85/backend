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
                throw new Exception("Danh sách company không được rỗng.");

            foreach (var company in request.Companies)
            {
                if (company.SmallPoints == null || !company.SmallPoints.Any())
                    throw new Exception($"Company {company.CompanyId} không có smallPoints.");

                foreach (var sp in company.SmallPoints)
                {
                    if (sp.RadiusKm <= 0)
                        throw new Exception($"SmallPoint {sp.SmallPointId} radiusKm không hợp lệ.");

                    if (sp.MaxRoadDistanceKm <= 0)
                        throw new Exception($"SmallPoint {sp.SmallPointId} maxRoadDistanceKm không hợp lệ.");
                }
            }

            FakeDataSeeder.CompanyConfigs = request.Companies;

            var companyDtos = request.Companies.Select(company =>
            {
                var realCompany = FakeDataSeeder.collectionTeams
                    .FirstOrDefault(t => t.Id == company.CompanyId);

                string companyName = realCompany?.Name ?? $"Company {company.CompanyId}";

                var smallPointDtos = company.SmallPoints.Select(cfgSp =>
                {
                    var realSP = FakeDataSeeder.smallCollectionPoints
                        .FirstOrDefault(p => p.Id == cfgSp.SmallPointId);

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
                    .FirstOrDefault(t => t.Id == company.CompanyId);

                string companyName = realCompany?.Name ?? $"Company {company.CompanyId}";

                var smallPoints = company.SmallPoints.Select(cfgSp =>
                {
                    var realSP = FakeDataSeeder.smallCollectionPoints
                        .FirstOrDefault(p => p.Id == cfgSp.SmallPointId);

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
