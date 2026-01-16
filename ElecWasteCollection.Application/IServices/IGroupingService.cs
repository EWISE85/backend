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
        Task<List<object>> GetGroupsByPointIdAsync(string collectionPointId);
        Task<object> GetRoutesByGroupAsync(int groupId);
        Task<List<Vehicles>> GetVehiclesAsync();
        Task<List<PendingPostModel>> GetPendingPostsAsync();
        Task<List<Vehicles>> GetVehiclesBySmallPointAsync(string smallPointId);
        Task<SinglePointSettingResponse> GetPointSettingAsync(string pointId);
        Task<CompanySettingsResponse> GetCompanySettingsAsync(string companyId);
        Task<bool> UpdatePointSettingAsync(UpdatePointSettingRequest request);
        Task<object> GetPreviewProductsAsync(string vehicleId, DateOnly workDate);
        Task<object> GetPreviewVehiclesAsync(DateOnly workDate);
    }
}
