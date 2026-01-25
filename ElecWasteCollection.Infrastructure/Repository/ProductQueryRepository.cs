using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using ElecWasteCollection.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;


namespace ElecWasteCollection.Infrastructure.Repository
{
    public class ProductQueryRepository : IProductQueryRepository
    {
        private readonly ElecWasteCollectionDbContext _context;

        public ProductQueryRepository(ElecWasteCollectionDbContext context)
        {
            _context = context;
        }
        public async Task<(List<Products> Items, int TotalCount)>
    GetPagedSmallPointProductsAsync(
        string smallPointId,
        DateOnly workDate,
        int page,
        int limit)
        {
            var baseQuery = _context.Products
                .AsNoTracking()
                .Where(p =>
                    p.SmallCollectionPointId == smallPointId &&
                    p.Status == ProductStatus.CHO_GOM_NHOM.ToString() &&
                    p.CreateAt == workDate);

            var totalCount = await baseQuery.CountAsync();

            var products = await baseQuery
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.User)
                .Include(p => p.Posts)
                .OrderBy(p => p.CreateAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return (products, totalCount);
        }
    }
}
