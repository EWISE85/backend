using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;

namespace ElecWasteCollection.Application.Services
{
    public class RecyclingQueryService : IRecyclingQueryService
    {
        private readonly IUnitOfWork _unitOfWork;
		private readonly IPackageRepository _packageRepository;

		public RecyclingQueryService(IUnitOfWork unitOfWork, IPackageRepository packageRepository)
		{
			_unitOfWork = unitOfWork;
			_packageRepository = packageRepository;
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
                    .Where(p => p.Status == PackageStatus.DA_DONG_THUNG.ToString())
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
                            Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PackageStatus>(p.Status),
                            CreateAt = p.CreateAt
                        }).ToList()
                    });
                }
            }

            return result.OrderByDescending(x => x.TotalPackage).ToList();
        }
		public async Task<PagedResultModel<PackageDetailModel>> GetPackagesByRecyclerFilterAsync(RecyclerPackageFilterModel query)
		{
			string? searchStatusEnum = null;
			if (!string.IsNullOrEmpty(query.Status))
			{
				
				try
				{
					var statusValue = StatusEnumHelper.GetValueFromDescription<PackageStatus>(query.Status);
					searchStatusEnum = statusValue.ToString();
				}
				catch
				{
					searchStatusEnum = null;
				}
			}

			
			var (pagedPackages, totalCount) = await _packageRepository.GetPagedPackagesWithDetailsByRecyclerAsync(
				query.RecyclingCompanyId,
				searchStatusEnum,
				query.Page,
				query.Limit
			);

			
			var resultItems = pagedPackages.Select(pkg =>
			{
				
				int totalProds = pkg.Products?.Count ?? 0;

				
				var productsSummary = new PagedResultModel<ProductDetailModel>(
					new List<ProductDetailModel>(),
					1, 0, totalProds
				);

				return new PackageDetailModel
				{
					PackageId = pkg.PackageId,
					SmallCollectionPointsId = pkg.SmallCollectionPointsId,
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PackageStatus>(pkg.Status),

					SmallCollectionPointsName = pkg.SmallCollectionPoints?.Name ?? "Không xác định",
					SmallCollectionPointsAddress = pkg.SmallCollectionPoints?.Address ?? "Không xác định",

					Products = productsSummary
				};

			}).ToList();

			return new PagedResultModel<PackageDetailModel>(
				resultItems,
				query.Page,
				query.Limit,
				totalCount
			);
		}
	}
}
