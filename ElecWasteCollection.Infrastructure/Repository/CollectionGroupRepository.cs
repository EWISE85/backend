using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using ElecWasteCollection.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace ElecWasteCollection.Infrastructure.Repository
{
    public class CollectionGroupRepository
        : ICollectionGroupRepository
    {
        private readonly ElecWasteCollectionDbContext _context;

        public CollectionGroupRepository(ElecWasteCollectionDbContext context)
        {
            _context = context;
        }

        public async Task<(List<CollectionGroups> Items, int TotalCount)>
            GetPagedGroupsByCollectionPointAsync(
                string collectionPointId,
                int page,
                int limit)
        {
            IQueryable<CollectionGroups> query = _context.CollectionGroups
                .AsNoTracking()
                .AsSplitQuery()
                .Include(g => g.Shifts)
                    .ThenInclude(s => s.Vehicle)
                .Include(g => g.Shifts)
                    .ThenInclude(s => s.Collector)
                .Where(g =>
                    (g.Shifts.Vehicle != null &&
                     g.Shifts.Vehicle.Small_Collection_Point == collectionPointId)
                    ||
                    (g.Shifts.Collector != null &&
                     g.Shifts.Collector.SmallCollectionPointId == collectionPointId)
                );

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(g => g.Created_At)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToListAsync();

            return (items, totalCount);
        }


    }
}
