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
        public async Task<PagedResult<PackageDetailModel>> GetPackagesByRecyclerFilterAsync(RecyclerPackageFilterModel query)
        {

            string includeProps = "SmallCollectionPoints,Products,Products.Brand,Products.Category";

            string searchStatus = query.Status?.Trim().ToLower();

            var allPackages = await _unitOfWork.Packages.GetAllAsync(
                filter: p => p.SmallCollectionPoints.RecyclingCompanyId == query.RecyclingCompanyId &&
                    (string.IsNullOrEmpty(searchStatus) || p.Status.ToLower().Contains(searchStatus)), 
                includeProperties: includeProps
            );

            var totalCount = allPackages.Count();

            var pagedPackages = allPackages
                .OrderByDescending(p => p.CreateAt)
                .Skip((query.Page - 1) * query.Limit)
                .Take(query.Limit)
                .ToList();

            var resultItems = pagedPackages.Select(pkg => new PackageDetailModel
            {
                PackageId = pkg.PackageId,
                SmallCollectionPointsId = pkg.SmallCollectionPointsId,
                Status = pkg.Status,

                SmallCollectionPointsName = pkg.SmallCollectionPoints?.Name ?? "Không xác định",
                SmallCollectionPointsAddress = pkg.SmallCollectionPoints?.Address ?? "Không xác định",

                Products = pkg.Products?.Select(prod => new ProductDetailModel
                {
                    ProductId = prod.ProductId,
                    QrCode = prod.QRCode,
                    Status = prod.Status,
                    Description = prod.Description,
                    BrandId = prod.BrandId,
                    BrandName = prod.Brand?.Name,
                    CategoryId = prod.CategoryId,
                    CategoryName = prod.Category?.Name,
                    IsChecked = prod.isChecked
                }).ToList() ?? new List<ProductDetailModel>()

            }).ToList();

            return new PagedResult<PackageDetailModel>
            {
                Data = resultItems,
                TotalItems = totalCount,
                Page = query.Page,
                Limit = query.Limit,
            };
        }
    }
}
