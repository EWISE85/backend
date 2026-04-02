using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface ICapacityService
    {
        Task<List<SCPCapacityModel>> GetAllSCPCapacityAsync();
        Task<SCPCapacityModel> GetSCPCapacityByIdAsync(string pointId);
        Task<CompanyCapacityModel> GetCompanyCapacitySummaryAsync(string companyId);
        Task<CompanyCapacityModel> GetCompanyCapacityByDateAsync(string companyId, DateOnly date);
    }
}
