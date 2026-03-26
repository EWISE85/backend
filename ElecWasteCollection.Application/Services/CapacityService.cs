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
    }
}