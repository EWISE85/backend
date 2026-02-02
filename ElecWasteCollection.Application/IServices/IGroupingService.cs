using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.GroupModel;
using ElecWasteCollection.Domain.Entities;

namespace ElecWasteCollection.Application.Interfaces
{
    public interface IGroupingService
    {
        Task<PreAssignResponse> PreAssignAsync(PreAssignRequest request);
        Task<bool> AssignDayAsync(AssignDayRequest request);
        Task<GroupingByPointResponse> GroupByCollectionPointAsync(GroupingByPointRequest request);
        Task<List<Vehicles>> GetVehiclesAsync();
        Task<List<PendingPostModel>> GetPendingPostsAsync();
        Task<List<Vehicles>> GetVehiclesBySmallPointAsync(string smallPointId);
        Task<SinglePointSettingResponse> GetPointSettingAsync(string pointId);
        Task<bool> UpdatePointSettingAsync(UpdatePointSettingRequest request);
        Task<object> GetPreviewVehiclesAsync(string collectionPointId, DateOnly workDate);
        Task<PreviewProductPagedResult?> GetPreviewProductsAsync(string vehicleId, DateOnly workDate, int page, int pageSize);
        Task<PagedResult<CollectionGroupModel>> GetGroupsByCollectionPointAsync(string collectionPointId, int page, int limit);
        Task<object> GetRoutesByGroupAsync(int groupId, int page, int limit);
        Task<PagedCompanySettingsResponse> GetCompanySettingsPagedAsync(string companyId, int page, int limit);

        public Task<object> GetUnassignedProductsAsync(
    string collectionPointId,
    DateOnly workDate,
    int page,
    int pageSize);
    }

}
