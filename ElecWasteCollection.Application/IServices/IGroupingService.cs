using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.GroupModel;

namespace ElecWasteCollection.Application.Interfaces
{
    public interface IGroupingService
    {
        //Task<GroupingByPointResponse> GroupByCollectionPointAsync(GroupingByPointRequest request);
        //Task<PreAssignResponse> PreAssignAsync(PreAssignRequest request);
        //Task<bool> AssignDayAsync(AssignDayRequest request);


        Task<PreAssignResponse> PreAssignAsync(PreAssignRequest request);
        Task<bool> AssignDayAsync(AssignDayRequest request);
        Task<GroupingByPointResponse> GroupByCollectionPointAsync(GroupingByPointRequest request);



    }
}
