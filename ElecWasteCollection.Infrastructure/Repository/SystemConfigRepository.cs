using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using ElecWasteCollection.Infrastructure.Context;
using Microsoft.EntityFrameworkCore; 
using ElecWasteCollection.Infrastructure.Repository;

public class SystemConfigRepository : GenericRepository<SystemConfig>, ISystemConfigRepository
{
    public SystemConfigRepository(ElecWasteCollectionDbContext context) : base(context) { }

    public async Task<List<SystemConfig>> GetActiveConfigsByFilterAsync(string? groupName, string? companyId, string? scpId)
    {
        var query = _dbSet.AsNoTracking()
            .Where(c => c.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString());

        if (!string.IsNullOrEmpty(groupName))
            query = query.Where(c => c.GroupName == groupName);

        if (!string.IsNullOrEmpty(companyId))
            query = query.Where(c => c.CompanyId == companyId);

        if (!string.IsNullOrEmpty(scpId))
            query = query.Where(c => c.CollectionUnitId == scpId);

        return await query.ToListAsync();
    }
}