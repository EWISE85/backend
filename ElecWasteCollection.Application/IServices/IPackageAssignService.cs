using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface IPackageAssignService
    {
        Task<List<RecyclingCompanyDto>> GetRecyclingCompaniesAsync();
        Task AssignScpToCompanyAsync(List<AssignScpToCompanyRequest> requests);
        Task UpdateScpAssignmentAsync(string scpId, string newCompanyId);
        Task<List<CollectionCompanyGroupDto>> GetAssignmentOverviewAsync();
        Task<ScpAssignmentDetailDto> GetScpAssignmentDetailAsync(string companyId);
    }
}
