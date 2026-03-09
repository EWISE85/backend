using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;

namespace ElecWasteCollection.Application.Helper
{
    public class CapacityHelper
    {
        private readonly IUnitOfWork _unitOfWork;

        public CapacityHelper(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task SyncRealtimeCapacityAsync(string pointId)
        {
            if (string.IsNullOrEmpty(pointId)) return;

            // 1. Lấy Map ID của các thuộc tính kích thước
            var attIdMap = await GetAttributeIdMapInternalAsync();

            // 2. Chỉ lấy sản phẩm ĐANG CÓ MẶT tại kho (Nhập kho hoặc Đã đóng thùng)
            var inventoryProducts = await _unitOfWork.Products.GetAllAsync(p =>
                p.SmallCollectionPointId == pointId &&
                (p.Status == ProductStatus.NHAP_KHO.ToString() ||
                 p.Status == ProductStatus.DA_DONG_THUNG.ToString()),
                 includeProperties: "ProductValues");

            double totalRealVolume = 0;

            foreach (var p in inventoryProducts)
            {
                totalRealVolume += CalculateVolume(p.ProductValues.ToList(), attIdMap);
            }

            // 3. Cập nhật CurrentCapacity cho SmallCollectionPoint
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(pointId);
            if (point != null)
            {
                point.CurrentCapacity = Math.Round(totalRealVolume, 4);
                _unitOfWork.SmallCollectionPoints.Update(point);

                // SaveAsync tại đây để đảm bảo dung lượng được chốt vào DB ngay lập tức
                await _unitOfWork.SaveAsync();
            }
        }

        private double CalculateVolume(List<ProductValues> pValues, Dictionary<string, Guid> attMap)
        {
            double l = 0, w = 0, h = 0;

            // Tìm giá trị theo AttributeId tương ứng từ map
            if (attMap.TryGetValue("Chiều dài", out Guid lId))
                l = pValues.FirstOrDefault(v => v.AttributeId == lId)?.Value ?? 0;
            if (attMap.TryGetValue("Chiều rộng", out Guid wId))
                w = pValues.FirstOrDefault(v => v.AttributeId == wId)?.Value ?? 0;
            if (attMap.TryGetValue("Chiều cao", out Guid hId))
                h = pValues.FirstOrDefault(v => v.AttributeId == hId)?.Value ?? 0;

            // Công thức: (Dài x Rộng x Cao) / 1,000,000 để đổi từ cm3 sang m3
            double vol = (l * w * h) / 1000000.0;

            // Fallback nếu dữ liệu kích thước bằng 0 (tránh kho trống dù có hàng)
            if (vol <= 0) vol = 0.001;

            return Math.Round(vol, 5);
        }

        private async Task<Dictionary<string, Guid>> GetAttributeIdMapInternalAsync()
        {
            var targets = new[] { "Chiều dài", "Chiều rộng", "Chiều cao" };
            var all = await _unitOfWork.Attributes.GetAllAsync();

            return all.Where(a => targets.Any(t => a.Name.Contains(t, StringComparison.OrdinalIgnoreCase)))
                      .ToDictionary(
                        a => targets.First(t => a.Name.Contains(t, StringComparison.OrdinalIgnoreCase)),
                        a => a.AttributeId
                      );
        }
    }
}