using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;

namespace ElecWasteCollection.Application.Services
{
    public class VehiAndSCPManagementService : IVehiAndSCPManagementService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IVehicleRepository _vehicleRepository;
        private readonly ISmallCollectionRepository _smallCollectionRepository;

        public VehiAndSCPManagementService(
            IUnitOfWork unitOfWork,
            IVehicleRepository vehicleRepository,
            ISmallCollectionRepository smallCollectionRepository)
        {
            _unitOfWork = unitOfWork;
            _vehicleRepository = vehicleRepository;
            _smallCollectionRepository = smallCollectionRepository;
        }

        #region Vehicle Management
        public async Task<bool> ApproveVehicleAsync(string vehicleId)
        {
            var vehicle = await _vehicleRepository.GetAsync(v => v.VehicleId == vehicleId);

            if (vehicle == null) throw new AppException("Xe không tồn tại", 404);

            var parentPoint = await _smallCollectionRepository.GetAsync(p => p.SmallCollectionPointsId == vehicle.Small_Collection_Point);
            if (parentPoint == null)
                throw new AppException("Không thể duyệt xe vì chưa gán điểm thu gom hợp lệ.", 400);

            if (parentPoint.Status != SmallCollectionPointStatus.DANG_HOAT_DONG.ToString())
                throw new AppException("Không thể duyệt xe khi điểm thu gom chủ quản đang bị khóa hoặc bảo trì.", 400);

            if (vehicle.Status == VehicleStatus.DANG_HOAT_DONG.ToString()) return true;

            vehicle.Status = VehicleStatus.DANG_HOAT_DONG.ToString();
            _unitOfWork.Vehicles.Update(vehicle);
            return await _unitOfWork.SaveAsync() > 0;
        }

        public async Task<bool> BlockVehicleAsync(string vehicleId)
        {
            var vehicle = await _vehicleRepository.GetAsync(v => v.VehicleId == vehicleId);
            if (vehicle == null) throw new AppException("Xe không tồn tại", 404);

            vehicle.Status = VehicleStatus.KHONG_HOAT_DONG.ToString();
            _unitOfWork.Vehicles.Update(vehicle);
            return await _unitOfWork.SaveAsync() > 0;
        }
        #endregion

        #region Small Collection Point Management
        public async Task<bool> ApproveSmallCollectionPointAsync(string pointId)
        {
            var point = await _smallCollectionRepository.GetAsync(p => p.SmallCollectionPointsId == pointId);
            if (point == null) throw new AppException("Điểm thu gom không tồn tại", 404);;

            point.Status = SmallCollectionPointStatus.DANG_HOAT_DONG.ToString();
            point.Updated_At = DateTime.UtcNow;

            _unitOfWork.SmallCollectionPoints.Update(point);
            return await _unitOfWork.SaveAsync() > 0;
        }

        public async Task<bool> BlockSmallCollectionPointAsync(string pointId)
        {
            var point = await _smallCollectionRepository.GetAsync(p => p.SmallCollectionPointsId == pointId, includeProperties: "Vehicles");
            if (point == null) throw new AppException("Điểm thu gom không tồn tại", 404);

            if (point.Vehicles.Any(v => v.Status == VehicleStatus.DANG_HOAT_DONG.ToString()))
            {
                throw new AppException("Vẫn còn xe đang hoạt động tại điểm này. Hãy khóa tất cả xe trước khi khóa điểm thu gom.", 400);
            }

            point.Status = SmallCollectionPointStatus.KHONG_HOAT_DONG.ToString();
            point.Updated_At = DateTime.UtcNow;

            _unitOfWork.SmallCollectionPoints.Update(point);
            return await _unitOfWork.SaveAsync() > 0;
        }
        #endregion
    }
}