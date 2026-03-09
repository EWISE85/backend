using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
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

        // Lấy tất cả kho: Chỉ cần Query trực tiếp cột trong DB
        public async Task<List<SCPCapacityModel>> GetAllSCPCapacityAsync()
        {
            var points = await _unitOfWork.SmallCollectionPoints.GetAllAsync();

            return points.Select(p => new SCPCapacityModel
            {
                Id = p.SmallCollectionPointsId,
                Name = p.Name,
                MaxCapacity = p.MaxCapacity,
                CurrentCapacity = p.CurrentCapacity // Lấy trực tiếp từ DB
            }).ToList();
        }

        // Lấy 1 kho cụ thể
        public async Task<SCPCapacityModel> GetSCPCapacityByIdAsync(string pointId)
        {
            var p = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(pointId)
                ?? throw new Exception("Trạm thu gom không tồn tại.");

            return new SCPCapacityModel
            {
                Id = p.SmallCollectionPointsId,
                Name = p.Name,
                MaxCapacity = p.MaxCapacity,
                CurrentCapacity = p.CurrentCapacity // Lấy trực tiếp từ DB
            };
        }

        // Lấy tổng công ty
        public async Task<CompanyCapacityModel> GetCompanyCapacitySummaryAsync(string companyId)
        {
            var points = await _unitOfWork.SmallCollectionPoints.GetAllAsync(p => p.CompanyId == companyId);

            var model = new CompanyCapacityModel { CompanyId = companyId };

            foreach (var p in points)
            {
                var scpModel = new SCPCapacityModel
                {
                    Id = p.SmallCollectionPointsId,
                    Name = p.Name,
                    MaxCapacity = p.MaxCapacity,
                    CurrentCapacity = p.CurrentCapacity // Lấy trực tiếp từ DB
                };
                model.Warehouses.Add(scpModel);
                model.CompanyMaxCapacity += p.MaxCapacity;
                model.CompanyCurrentCapacity += p.CurrentCapacity;
            }

            return model;
        }
    }
}