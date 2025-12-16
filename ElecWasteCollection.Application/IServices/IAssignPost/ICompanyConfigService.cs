using ElecWasteCollection.Application.Model.AssignPost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices.IAssignPost
{

    public interface ICompanyConfigService
    {
        Task<CompanyConfigResponse> UpdateCompanyConfigAsync(CompanyConfigRequest request);
        Task<CompanyConfigResponse> GetCompanyConfigAsync();
    }
}
