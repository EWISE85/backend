using ElecWasteCollection.Domain.IRepository;
using ElecWasteCollection.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                .Where(p => p.SmallCollectionPointsId == scpId)
                .CountAsync(p => p.CreateAt >= fromUtc && p.CreateAt <= toUtc);
        }
        public async Task<List<DateTime>> GetPackageCreationDatesByScpIdAsync(string scpId, DateTime fromUtc, DateTime toUtc)
        {
            return await _context.Packages
                .Where(p => p.SmallCollectionPointsId == scpId && p.CreateAt >= fromUtc && p.CreateAt <= toUtc)
                .Select(p => p.CreateAt)
                .ToListAsync();
        }
        public async Task<int> CountProductsByScpIdAsync(string scpId, DateOnly from, DateOnly to)
        {
            return await _context.Products
                .Where(p => p.SmallCollectionPointId == scpId)
                .CountAsync(p => p.CreateAt >= from && p.CreateAt <= to);
        }
        public async Task<Dictionary<string, int>> GetProductCountsByCategoryByScpIdAsync(string scpId, DateOnly from, DateOnly to)
        {
            return await _context.Products
                .Where(p => p.SmallCollectionPointId == scpId && p.CreateAt >= from && p.CreateAt <= to)
                .GroupBy(p => p.Category.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToDictionaryAsync(k => k.Name, v => v.Count);
        }
    }
}