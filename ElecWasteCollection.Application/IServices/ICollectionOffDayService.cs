using ElecWasteCollection.Application.Model;

namespace ElecWasteCollection.Application.IServices
{
    namespace ElecWasteCollection.Application.IServices
    {
        public interface ICollectionOffDayService
        {
            Task<bool> RegisterOffDaysAsync(RegisterOffDayRequest request);
            Task<bool> RemoveOffDayAsync(string? companyId, string? pointId, DateOnly date);
            Task<List<CompanyAvailableModel>> GetAvailableCompaniesForAssignAsync(DateOnly workDate);
            Task<PagedResult<CollectionOffDayModel>> GetAllOffDaysAsync(
                string? companyId,
                DateOnly? date,
                string? smallCollectionPointId,
                int page,
                int limit);
        }
    }
}
