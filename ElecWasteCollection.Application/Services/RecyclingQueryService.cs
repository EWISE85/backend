using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
    public class RecyclingQueryService : IRecyclingQueryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RecyclingQueryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<RecyclerCollectionTaskDto>> GetPackagesToCollectAsync(string recyclingCompanyId)
        {
            var assignedScps = await _unitOfWork.SmallCollectionPoints.GetAllAsync(
                filter: s => s.RecyclingCompanyId == recyclingCompanyId,
                includeProperties: "Packages"
            );

            var result = new List<RecyclerCollectionTaskDto>();

            foreach (var scp in assignedScps)
            {
                var readyPackages = scp.Packages
                    .Where(p => p.Status == "Đã đóng thùng")
                    .OrderBy(p => p.CreateAt)
                    .ToList();

                if (readyPackages.Any())
                {
                    result.Add(new RecyclerCollectionTaskDto
                    {
                        SmallCollectionPointId = scp.SmallCollectionPointsId,
                        SmallCollectionName = scp.Name,
                        Address = scp.Address,
                        TotalPackage = readyPackages.Count,
                        Packages = readyPackages.Select(p => new PackageSimpleDto
                        {
                            PackageId = p.PackageId,
                            Status = p.Status,
                            CreateAt = p.CreateAt
                        }).ToList()
                    });
                }
            }

            return result.OrderByDescending(x => x.TotalPackage).ToList();
        }
    }
}
