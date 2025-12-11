using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface IReassignDriverService
    {
        Task<List<ReassignCandidateDto>> GetReassignCandidatesAsync(string companyId, DateTime workDate);
        Task<ReassignDriverResponse> ReassignDriverAsync(ReassignDriverRequest request);
    }
}
