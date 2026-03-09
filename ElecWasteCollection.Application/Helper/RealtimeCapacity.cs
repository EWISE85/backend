using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            var attIdMap = await GetAttributeIdMapInternalAsync();

            var inventoryProducts = await _unitOfWork.Products.GetAllAsync(p =>
                p.SmallCollectionPointId == pointId &&
                (p.Status == ProductStatus.NHAP_KHO.ToString() ||
                 p.Status == ProductStatus.DA_DONG_THUNG.ToString()));

            double totalRealVolume = 0;
            foreach (var p in inventoryProducts)
            {
                var metrics = await GetProductMetricsInternalAsync(p.ProductId, attIdMap);
                totalRealVolume += metrics.volume;
            }

            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(pointId);
            if (point != null)
            {
                point.CurrentCapacity = Math.Round(totalRealVolume, 4);
                _unitOfWork.SmallCollectionPoints.Update(point);
                await _unitOfWork.SaveAsync();
            }
        }

        #region Private Logic (Metrics Calculation)

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

        private async Task<(double weight, double volume)> GetProductMetricsInternalAsync(Guid productId, Dictionary<string, Guid> attMap)
        {
            var pValues = (await _unitOfWork.ProductValues.GetAllAsync(v => v.ProductId == productId)).ToList();
            double l = 0, w = 0, h = 0;

            if (attMap.TryGetValue("Chiều dài", out Guid lId)) l = pValues.FirstOrDefault(v => v.AttributeId == lId)?.Value ?? 0;
            if (attMap.TryGetValue("Chiều rộng", out Guid wId)) w = pValues.FirstOrDefault(v => v.AttributeId == wId)?.Value ?? 0;
            if (attMap.TryGetValue("Chiều cao", out Guid hId)) h = pValues.FirstOrDefault(v => v.AttributeId == hId)?.Value ?? 0;

            double vol = (l * w * h) / 1000000.0; // cm3 -> m3

            if (vol <= 0) // Fallback lấy từ Option
            {
                var optVal = pValues.FirstOrDefault(v => v.AttributeOptionId.HasValue);
                if (optVal != null)
                {
                    var opt = await _unitOfWork.AttributeOptions.GetByIdAsync(optVal.AttributeOptionId.Value);
                    vol = opt?.EstimateVolume ?? 0.001;
                }
            }
            return (0, Math.Round(vol, 5));
        }

        #endregion
    }
}
