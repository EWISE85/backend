using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services.AssignPackageService
{
    public class PackageAssignService : IPackageAssignService
    {
        private readonly IUnitOfWork _unitOfWork;


        public PackageAssignService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        
        public async Task<List<RecyclingCompanyDto>> GetRecyclingCompaniesAsync()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync(
                filter: c => c.CompanyType == CompanyType.CTY_TAI_CHE.ToString()
            );

            return companies.Select(c => new RecyclingCompanyDto
            {
                CompanyId = c.CompanyId,
                Name = c.Name,
                Phone = c.Phone,  
                Address = c.Address
            }).ToList();
        }

        public async Task AssignScpToCompanyAsync(List<AssignScpToCompanyRequest> requests)
        {
            foreach (var req in requests)
            {
                if (string.IsNullOrWhiteSpace(req.RecyclingCompanyId))
                    throw new Exception("Có yêu cầu thiếu ID công ty tái chế.");

                if (req.SmallCollectionPointIds == null || !req.SmallCollectionPointIds.Any())
                    continue; 

                var company = await _unitOfWork.Companies.GetAsync(c => c.CompanyId == req.RecyclingCompanyId);
                if (company == null)
                    throw new Exception($"Không tìm thấy công ty có ID: {req.RecyclingCompanyId}");

                if (company.CompanyType != CompanyType.CTY_TAI_CHE.ToString())
                    throw new Exception($"Công ty '{company.Name}' không phải là công ty tái chế.");

                var scpsToAssign = await _unitOfWork.SmallCollectionPoints.GetAllAsync(
                    s => req.SmallCollectionPointIds.Contains(s.SmallCollectionPointsId)
                );

                foreach (var scp in scpsToAssign)
                {
                    scp.RecyclingCompanyId = req.RecyclingCompanyId;
                    _unitOfWork.SmallCollectionPoints.Update(scp);
                }
            }

            await _unitOfWork.SaveAsync();
        }

       
        public async Task UpdateScpAssignmentAsync(string scpId, string newCompanyId)
        {
            if (string.IsNullOrWhiteSpace(scpId))
                throw new Exception("ID điểm thu gom không hợp lệ.");

            if (string.IsNullOrWhiteSpace(newCompanyId))
                throw new Exception("Vui lòng chọn công ty tái chế mới.");

            var scp = await _unitOfWork.SmallCollectionPoints.GetAsync(s => s.SmallCollectionPointsId == scpId);
            if (scp == null)
                throw new Exception($"Không tìm thấy điểm thu gom có ID: {scpId}");

            if (scp.RecyclingCompanyId == newCompanyId)
                throw new Exception("Điểm thu gom này đang thuộc về công ty bạn chọn rồi. Không cần cập nhật.");

            var company = await _unitOfWork.Companies.GetAsync(c => c.CompanyId == newCompanyId);
            if (company == null)
                throw new Exception($"Không tìm thấy công ty có ID: {newCompanyId}");

            if (company.CompanyType != CompanyType.CTY_TAI_CHE.ToString())
                throw new Exception($"Công ty '{company.Name}' không phải là công ty tái chế.");

            scp.RecyclingCompanyId = newCompanyId;
            _unitOfWork.SmallCollectionPoints.Update(scp);

            await _unitOfWork.SaveAsync();
        }

        public async Task<List<CollectionCompanyGroupDto>> GetAssignmentOverviewAsync()
        {
            var collectionCompanies = await _unitOfWork.Companies.GetAllAsync(
                filter: c => c.CompanyType == CompanyType.CTY_THU_GOM.ToString(),
                includeProperties: "SmallCollectionPoints,SmallCollectionPoints.RecyclingCompany"
            );

            var result = collectionCompanies.Select(c => new CollectionCompanyGroupDto
            {
                CompanyId = c.CompanyId,
                CompanyName = c.Name,
                SmallPoints = c.SmallCollectionPoints.Select(scp => new ScpAssignmentStatusDto
                {
                    SmallPointId = scp.SmallCollectionPointsId,
                    Name = scp.Name,
                    Address = scp.Address,
                    RecyclingCompany = scp.RecyclingCompany == null ? null : new AssignedRecyclerDto
                    {
                        CompanyId = scp.RecyclingCompany.CompanyId,
                        Name = scp.RecyclingCompany.Name
                    }
                }).ToList()
            }).ToList();

            return result;
        }
        public async Task<ScpAssignmentDetailDto> GetScpAssignmentDetailAsync(string companyId)
        {
            var company = await _unitOfWork.Companies.GetAsync(
                filter: c => c.CompanyId == companyId,
                includeProperties: "SmallCollectionPoints.RecyclingCompany"
            );

            if (company == null)
            {
                throw new Exception($"Không tìm thấy công ty thu gom với ID: {companyId}");
            }

            var result = new ScpAssignmentDetailDto
            {
                CompanyId = company.CompanyId,
                CompanyName = company.Name,
                SmallPoints = company.SmallCollectionPoints?.Select(scp => new SmallPointDetailDto
                {
                    SmallPointId = scp.SmallCollectionPointsId,
                    Name = scp.Name,
                    Address = scp.Address,

                    RecyclingCompany = scp.RecyclingCompany == null ? null : new RecyclerSimpleInfoDto
                    {
                        CompanyId = scp.RecyclingCompany.CompanyId,
                        Name = scp.RecyclingCompany.Name
                    }
                }).ToList() ?? new List<SmallPointDetailDto>() 
            };

            return result;
        }


    }
}