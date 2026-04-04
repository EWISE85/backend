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

            var attIdMap = await GetAttributeIdMapInternalAsync();

            var allRelatedProducts = await _unitOfWork.Products.GetAllAsync(p =>
                p.CollectionUnitId == pointId,
                includeProperties: "ProductValues");

            double realVolume = 0;    
            double plannedVolume = 0; 

            foreach (var p in allRelatedProducts)
            {
                double vol = CalculateVolume(p.ProductValues.ToList(), attIdMap);

                if (p.Status == ProductStatus.NHAP_KHO.ToString() ||
                    p.Status == ProductStatus.DA_DONG_THUNG.ToString())
                {
                    realVolume += vol;
                }
                else if (p.Status == ProductStatus.CHO_GOM_NHOM.ToString() ||
                         p.Status == ProductStatus.CHO_THU_GOM.ToString())
                {
                    plannedVolume += vol;
                }
            }

            var point = await _unitOfWork.CollectionUnits.GetByIdAsync(pointId);
            if (point != null)
            {
                point.CurrentCapacity = Math.Round(realVolume, 4);
                point.PlannedCapacity = Math.Round(plannedVolume, 4);

                _unitOfWork.CollectionUnits.Update(point);
                await _unitOfWork.SaveAsync();
            }
        }

        private double CalculateVolume(List<ProductValues> pValues, Dictionary<string, Guid> attMap)
        {
            double l = 0, w = 0, h = 0;

            if (attMap.TryGetValue("Chiều dài", out Guid lId))
                l = pValues.FirstOrDefault(v => v.AttributeId == lId)?.Value ?? 0;
            if (attMap.TryGetValue("Chiều rộng", out Guid wId))
                w = pValues.FirstOrDefault(v => v.AttributeId == wId)?.Value ?? 0;
            if (attMap.TryGetValue("Chiều cao", out Guid hId))
                h = pValues.FirstOrDefault(v => v.AttributeId == hId)?.Value ?? 0;

            double vol = (l * w * h) / 1000000.0;

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