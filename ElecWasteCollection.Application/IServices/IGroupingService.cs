using ElecWasteCollection.Application.Model;

namespace ElecWasteCollection.Application.Interfaces
{
    public interface IGroupingService
    {
        Task<GroupingByPointResponse> GroupByCollectionPointAsync(GroupingByPointRequest request);
    }
}
