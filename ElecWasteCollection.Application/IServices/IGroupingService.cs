using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.GroupModel;
using ElecWasteCollection.Domain.Entities;
using static ElecWasteCollection.Application.Data.FakeDataSeeder;

namespace ElecWasteCollection.Application.Interfaces
{
    public interface IGroupingService
    {
        Task<PreAssignResponse> PreAssignAsync(PreAssignRequest request);
        Task<bool> AssignDayAsync(AssignDayRequest request);
        Task<GroupingByPointResponse> GroupByCollectionPointAsync(GroupingByPointRequest request);
        Task<List<object>> GetGroupsByPointIdAsync(int collectionPointId);
        Task<object> GetRoutesByGroupAsync(int groupId);
        Task<List<Vehicles>> GetVehiclesAsync();
        Task<List<PendingPostModel>> GetPendingPostsAsync();

    }
}
