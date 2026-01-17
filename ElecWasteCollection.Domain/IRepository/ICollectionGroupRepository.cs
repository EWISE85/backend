using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
    public interface ICollectionGroupRepository
    {
        Task<(List<CollectionGroups> Items, int TotalCount)> GetPagedGroupsByCollectionPointAsync( string collectionPointId, int page, int limit);
    }
}
