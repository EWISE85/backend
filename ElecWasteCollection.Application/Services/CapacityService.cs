using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;

namespace ElecWasteCollection.Application.Services
{
    public class CapacityService : ICapacityService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CapacityService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<SCPCapacityModel>> GetAllSCPCapacityAsync()
        {
            var points = await _unitOfWork.SmallCollectionPoints.GetAllAsync();

            return points.Select(p => new SCPCapacityModel
            {
                Id = p.SmallCollectionPointsId,
                Name = p.Name,
                MaxCapacity = p.MaxCapacity,
                CurrentCapacity = p.CurrentCapacity 
            }).ToList();
        }

        public async Task<SCPCapacityModel> GetSCPCapacityByIdAsync(string pointId)
        {
            var p = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(pointId)
                ?? throw new Exception("Trạm thu gom không tồn tại.");

            return new SCPCapacityModel
            {
                Id = p.SmallCollectionPointsId,
                Name = p.Name,
                MaxCapacity = p.MaxCapacity,
                CurrentCapacity = p.CurrentCapacity 
            };
        }

        public async Task<CompanyCapacityModel> GetCompanyCapacitySummaryAsync(string companyId)
        {
            var allPoints = await _unitOfWork.SmallCollectionPoints.GetAllAsync(p => p.CompanyId == companyId);

            var activePoints = allPoints.Where(p => p.Status == SmallCollectionPointStatus.DANG_HOAT_DONG.ToString()).ToList();

            var model = new CompanyCapacityModel
            {
                CompanyId = companyId,
                Warehouses = new List<SCPCapacityModel>()
            };

            foreach (var p in activePoints)
            {
                var scpModel = new SCPCapacityModel
                {
                    Id = p.SmallCollectionPointsId,
                    Name = p.Name,
                    MaxCapacity = p.MaxCapacity,
                    CurrentCapacity = p.CurrentCapacity
                };

                model.Warehouses.Add(scpModel);
                model.CompanyMaxCapacity += p.MaxCapacity;
                model.CompanyCurrentCapacity += p.CurrentCapacity;
            }
            return model;
        }

        public async Task<CompanyCapacityModel> GetCompanyCapacityByDateAsync(string companyId, DateOnly date)
        {
            var attMap = await GetAttributeIdMapInternalAsync();

            var allPoints = await _unitOfWork.SmallCollectionPoints.GetAllAsync(p => p.CompanyId == companyId);

            var activePoints = allPoints
                .Where(p => p.Status == SmallCollectionPointStatus.DANG_HOAT_DONG.ToString())
                .ToList();

            var activePointIds = activePoints.Select(p => p.SmallCollectionPointsId).ToList();

            var productsInDate = await _unitOfWork.Products.GetAllAsync(p =>
                p.SmallCollectionPointId != null &&
                activePointIds.Contains(p.SmallCollectionPointId) &&
                p.AssignedAt.HasValue &&
                p.AssignedAt.Value == date);

            var model = new CompanyCapacityModel
            {
                CompanyId = companyId,
                Warehouses = new List<SCPCapacityModel>()
            };

            foreach (var p in activePoints)
            {
                double dailyTotalVol = 0;
                var scpProducts = productsInDate.Where(prod => prod.SmallCollectionPointId == p.SmallCollectionPointsId);

                foreach (var prod in scpProducts)
                {
                    dailyTotalVol += await CalculateProductVolumeAsync(prod.ProductId, attMap);
                }

                model.Warehouses.Add(new SCPCapacityModel
                {
                    Id = p.SmallCollectionPointsId,
                    Name = p.Name,
                    MaxCapacity = Math.Round(p.MaxCapacity, 2),
                    CurrentCapacity = Math.Round(p.CurrentCapacity, 2),
                    AvailableCapacity = Math.Round(p.MaxCapacity - p.CurrentCapacity, 2),
                    PlannedCapacity = Math.Round(p.PlannedCapacity, 2),
                    AddedVolumeThisDate = Math.Round(dailyTotalVol, 2)
                });

                model.CompanyMaxCapacity += p.MaxCapacity;
                model.CompanyCurrentCapacity += p.CurrentCapacity;
                model.CompanyTotalAddedToday += dailyTotalVol;
            }

            model.CompanyMaxCapacity = Math.Round(model.CompanyMaxCapacity, 2);
            model.CompanyCurrentCapacity = Math.Round(model.CompanyCurrentCapacity, 2);
            model.CompanyTotalAddedToday = Math.Round(model.CompanyTotalAddedToday, 2);

            return model;
        }

        private async Task<double> CalculateProductVolumeAsync(Guid productId, Dictionary<string, Guid> attMap)
        {
            var pValues = (await _unitOfWork.ProductValues.GetAllAsync(v => v.ProductId == productId)).ToList();

            double l = attMap.ContainsKey("Chiều dài") ? (pValues.FirstOrDefault(v => v.AttributeId == attMap["Chiều dài"])?.Value ?? 0) : 0;
            double w = attMap.ContainsKey("Chiều rộng") ? (pValues.FirstOrDefault(v => v.AttributeId == attMap["Chiều rộng"])?.Value ?? 0) : 0;
            double h = attMap.ContainsKey("Chiều cao") ? (pValues.FirstOrDefault(v => v.AttributeId == attMap["Chiều cao"])?.Value ?? 0) : 0;

            double vol = (l * w * h) / 1000000.0;

            if (vol <= 0)
            {
                var optVal = pValues.FirstOrDefault(v => v.AttributeOptionId.HasValue);
                if (optVal != null)
                {
                    var opt = await _unitOfWork.AttributeOptions.GetByIdAsync(optVal.AttributeOptionId.Value);
                    vol = opt?.EstimateVolume ?? 0.001;
                }
            }
            return vol;
        }

        private async Task<Dictionary<string, Guid>> GetAttributeIdMapInternalAsync()
        {
            var targets = new[] { "Chiều dài", "Chiều rộng", "Chiều cao", "Dung tích" };
            var all = await _unitOfWork.Attributes.GetAllAsync();
            var map = new Dictionary<string, Guid>();
            foreach (var k in targets)
            {
                var m = all.FirstOrDefault(a => a.Name.Contains(k, StringComparison.OrdinalIgnoreCase));
                if (m != null) map.Add(k, m.AttributeId);
            }
            return map;
        }
    }
}