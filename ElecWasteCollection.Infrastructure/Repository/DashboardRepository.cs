using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using ElecWasteCollection.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace ElecWasteCollection.Infrastructure.Repository
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly ElecWasteCollectionDbContext _context;

        public DashboardRepository(ElecWasteCollectionDbContext context)
        {
            _context = context;
        }
        public async Task<int> CountUsersAsync(DateTime fromUtc, DateTime toUtc)
        {
            return await _context.Users
                .CountAsync(u => u.CreateAt >= fromUtc && u.CreateAt <= toUtc);
        }
        public async Task<int> CountCompaniesAsync(DateTime fromUtc, DateTime toUtc)
        {
            return await _context.Companies
                .CountAsync(c => c.Created_At >= fromUtc && c.Created_At <= toUtc);
        }
        public async Task<int> CountProductsAsync(DateOnly from, DateOnly to)
        {
            return await _context.Products
                .CountAsync(p => p.CreateAt >= from && p.CreateAt <= to);
        }
        public async Task<int> CountPackagesByScpIdAsync(string scpId, DateTime fromUtc, DateTime toUtc)
        {
            return await _context.Packages
                .Where(p => p.CollectionUnitId == scpId)
                .CountAsync(p => p.CreateAt >= fromUtc && p.CreateAt <= toUtc);
        }
        public async Task<List<DateTime>> GetPackageCreationDatesByScpIdAsync(string scpId, DateTime fromUtc, DateTime toUtc)
        {
            return await _context.Packages
                .Where(p => p.CollectionUnitId == scpId && p.CreateAt >= fromUtc && p.CreateAt <= toUtc)
                .Select(p => p.CreateAt)
                .ToListAsync();
        }
        public async Task<int> CountProductsByScpIdAsync(string scpId, DateOnly from, DateOnly to)
        {
            return await _context.Products
                .Where(p => p.CollectionUnitId == scpId)
                .CountAsync(p => p.CreateAt >= from && p.CreateAt <= to);
        }
        public async Task<Dictionary<string, int>> GetProductCountsByCategoryByScpIdAsync(string scpId, DateOnly from, DateOnly to)
        {
            return await _context.Products
                .Where(p => p.CollectionUnitId == scpId && p.CreateAt >= from && p.CreateAt <= to)
                .GroupBy(p => p.Category.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Name, v => v.Count);
        }
        

        public async Task<List<(Guid UserId, string Name, string Email, int ProductCount, double TotalPoints)>> GetTopUserStatsRawAsync(string scpId, int top, DateOnly from, DateOnly to)
        {
            var cleanId = scpId.Trim();

            var data = await _context.Users
                .Select(u => new
                {
                    u.UserId,
                    u.Name,
                    u.Email,
                    ProductCount = u.Products
                        .Count(p => p.CollectionUnitId == cleanId &&
                                    p.CreateAt >= from && p.CreateAt <= to),

                    TotalPoints = u.PointTransactions
                    .Where(t => (t.TransactionType == PointTransactionType.TICH_DIEM.ToString() ||
                             t.TransactionType == PointTransactionType.DIEU_CHINH.ToString()) &&
                             t.Product.CollectionUnitId == cleanId &&
                             t.Product.CreateAt >= from &&
                             t.Product.CreateAt <= to)
                .Sum(t => (double?)t.Point) ?? 0
                })
                .Where(x => x.ProductCount > 0)
                .OrderByDescending(x => x.TotalPoints)
                .Take(top)
                .ToListAsync();

            return data.Select(x => (x.UserId, x.Name ?? "N/A", x.Email ?? "N/A", x.ProductCount, x.TotalPoints)).ToList();
        }

        public async Task<List<(Guid ProductId, string CategoryName, string BrandName, string Status, double Point, DateOnly? CreateAt)>> GetUserProductDetailsRawAsync(Guid userId)
        {
            var data = await _context.Products
                .AsNoTracking()
                .Where(p => p.UserId == userId)
                .Select(p => new
                {
                    p.ProductId,
                    CategoryName = p.Category.Name,
                    BrandName = p.Brand.Name,
                    p.Status,
                    Point = p.PointTransactions
                    .Where(t => t.TransactionType == PointTransactionType.TICH_DIEM.ToString() ||
                            t.TransactionType == PointTransactionType.DIEU_CHINH.ToString())
                    .Sum(t => (double?)t.Point) ?? 0,
                    p.CreateAt
                })
                .OrderByDescending(p => p.CreateAt)
                .ToListAsync();

            return data.Select(x => (x.ProductId, x.CategoryName, x.BrandName, x.Status, x.Point, x.CreateAt)).ToList();
        }
        public async Task<List<(Guid UserId, string Name, string Email, int ProductCount, double TotalPoints)>> GetGlobalTopUserStatsRawAsync(int top, DateOnly from, DateOnly to)
        {
            var data = await _context.Users
                .Select(u => new
                {
                    u.UserId,
                    u.Name,
                    u.Email,
                    ProductCount = u.Products
                        .Count(p => p.CreateAt >= from && p.CreateAt <= to),

                    TotalPoints = u.PointTransactions
                    .Where(t => (t.TransactionType == PointTransactionType.TICH_DIEM.ToString() ||
                             t.TransactionType == PointTransactionType.DIEU_CHINH.ToString()) &&
                             t.Product.CreateAt >= from &&
                             t.Product.CreateAt <= to)
                    .Sum(t => (double?)t.Point) ?? 0

                })
                .Where(x => x.ProductCount > 0)
                .OrderByDescending(x => x.TotalPoints)
                .Take(top)
                .ToListAsync();

            return data.Select(x => (x.UserId, x.Name ?? "N/A", x.Email ?? "N/A", x.ProductCount, x.TotalPoints)).ToList();
        }
        public async Task<Dictionary<string, int>> GetProductCountsByBrandByScpIdAsync(string scpId, DateOnly from, DateOnly to)
        {
            return await _context.Products
                .Where(p => p.CollectionUnitId == scpId && p.CreateAt >= from && p.CreateAt <= to)
                .GroupBy(p => p.Brand.Name)
                .Select(g => new {
                    BrandName = g.Key ?? "N/A",
                    Count = g.Count()
                })
                .ToDictionaryAsync(k => k.BrandName, v => v.Count);
        }
        public async Task<Dictionary<string, int>> GetProductCountsByBrandAsync(DateOnly from, DateOnly to)
        {
            return await _context.Products
                .Where(p => p.CreateAt >= from && p.CreateAt <= to)
                .GroupBy(p => p.Brand.Name)
                .Select(g => new {
                    Name = g.Key ?? "N/A",
                    Count = g.Count()
                })
                .ToDictionaryAsync(k => k.Name, v => v.Count);
        }
        public async Task<(List<(string UserName, string CategoryName, double Point, DateOnly? CollectedDate, string ScpName, string Status)> Data, int TotalCount)> GetProductDetailsByBrandPagedRawAsync(string? scpId, string brandName, DateOnly from, DateOnly to, int page, int limit)
        {
            var query = _context.Products.AsNoTracking()
                .Where(p => p.Brand.Name == brandName && p.CreateAt >= from && p.CreateAt <= to);

            if (!string.IsNullOrEmpty(scpId) && scpId.ToUpper() != "ALL")
            {
                query = query.Where(p => p.CollectionUnitId == scpId.Trim());
            }

            int totalCount = await query.CountAsync();

            var rawData = await query
                .Select(p => new
                {
                    UserName = p.User.Name,
                    CategoryName = p.Category.Name,
                    TotalActualPoint = p.PointTransactions
                                .Where(t => t.TransactionType == PointTransactionType.TICH_DIEM.ToString()
                                         || t.TransactionType == PointTransactionType.DIEU_CHINH.ToString())
                                .Sum(t => (double?)t.Point) ?? 0,

                    EstimatePoint = p.Post != null ? p.Post.EstimatePoint : 0,


                    p.CreateAt,
                    ScpName = p.CollectionUnits != null ? p.CollectionUnits.Name : null,
                    p.Status
                })
                .OrderByDescending(p => p.CreateAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            var result = rawData.Select(x => (
                x.UserName ?? "N/A",
                x.CategoryName ?? "N/A",
                x.TotalActualPoint > 0 ? x.TotalActualPoint : x.EstimatePoint,
                x.CreateAt,
                x.ScpName ?? "Hiện chưa có đơn vị thu gom",
                x.Status
            )).ToList();

            return (result, totalCount);
        }
        public async Task<List<(string BrandName, string CategoryName, string UserName, string UserEmail, double Point, DateOnly? CollectedDate, string ScpName, string Status)>> GetExportDataRawAsync(DateOnly from, DateOnly to)
        {
            var rawData = await _context.Products
                .AsNoTracking()
                .Where(p => p.CreateAt >= from && p.CreateAt <= to)
                .Select(p => new
                {
                    BrandName = p.Brand.Name,
                    CategoryName = p.Category.Name,
                    UserName = p.User.Name,
                    UserEmail = p.User.Email,
                    TotalActualPoint = p.PointTransactions
                                .Where(t => t.TransactionType == PointTransactionType.TICH_DIEM.ToString()
                                         || t.TransactionType == PointTransactionType.DIEU_CHINH.ToString())
                                .Sum(t => (double?)t.Point) ?? 0,

                    EstimatePoint = p.Post != null ? p.Post.EstimatePoint : 0,


                    CollectedDate = p.CreateAt,
                    ScpName = p.CollectionUnits != null ? p.CollectionUnits.Name : null,
                    p.Status
                })
                .OrderBy(x => x.BrandName)
                .ThenByDescending(x => x.CollectedDate)
                .ToListAsync();

            return rawData.Select(x => (
                x.BrandName ?? "N/A",
                x.CategoryName ?? "N/A",
                x.UserName ?? "N/A",
                x.UserEmail ?? "N/A",
                x.TotalActualPoint > 0 ? x.TotalActualPoint : x.EstimatePoint,
                x.CollectedDate,
                x.ScpName ?? "Hiện chưa có điểm thu gom",
                x.Status
            )).ToList();
        }
        public async Task<List<(string ScpId, string ScpName, string ScheduleJson)>> GetOverdueRawDataAsync()
        {
            var data = await _context.Products
                .AsNoTracking()
                .Where(p => p.Status == ProductStatus.CHO_GOM_NHOM.ToString() && p.CollectionUnitId != null)
                .Select(p => new {
                    p.CollectionUnitId,
                    ScpName = p.CollectionUnits != null ? p.CollectionUnits.Name : "N/A",
                    Schedule = p.Post != null ? p.Post.ScheduleJson : null
                })
                .ToListAsync();

            return data.Select(x => (x.CollectionUnitId, x.ScpName, x.Schedule)).ToList();
        }

        public async Task<List<(Guid ProductId, string BrandName, string CategoryName, string UserName, string ScheduleJson, string Status)>>
            GetOverdueDetailRawAsync(string scpId)
        {
            var data = await _context.Products
                .AsNoTracking()
                .Where(p => p.Status == ProductStatus.CHO_GOM_NHOM.ToString() && p.CollectionUnitId == scpId)
                .Select(p => new {
                    p.ProductId,
                    BrandName = p.Brand.Name,
                    CategoryName = p.Category.Name,
                    UserName = p.User.Name,
                    Schedule = p.Post != null ? p.Post.ScheduleJson : null,
                    p.Status
                })
                .ToListAsync();

            return data.Select(x => (x.ProductId, x.BrandName, x.CategoryName, x.UserName, x.Schedule, x.Status)).ToList();
        }
    } 
}