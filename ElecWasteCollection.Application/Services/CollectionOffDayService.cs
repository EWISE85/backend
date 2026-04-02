using ElecWasteCollection.Application.IServices.ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.EntityFrameworkCore;
using static ElecWasteCollection.Application.Services.CollectionOffDayService;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.InteropServices;

namespace ElecWasteCollection.Application.Services
{
    public class CollectionOffDayService : ICollectionOffDayService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CollectionOffDayService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> RegisterOffDaysAsync(RegisterOffDayRequest request)
        {
            if (request.OffDates == null || !request.OffDates.Any())
                throw new Exception("Vui lòng chọn ít nhất một ngày nghỉ.");

            var today = DateOnly.FromDateTime(DateTime.Now);
            if (request.OffDates.Any(d => d < today))
                throw new Exception("Không thể đăng ký lịch nghỉ cho các ngày trong quá khứ.");

            bool isFullCompanyOff = request.SmallCollectionPointIds == null || !request.SmallCollectionPointIds.Any();

            foreach (var date in request.OffDates)
            {
                if (isFullCompanyOff)
                {
                    await AddUniqueOffDayAsync(date, request.CompanyId, null, request.Reason);
                }
                else
                {
                    foreach (var spId in request.SmallCollectionPointIds)
                    {
                        var point = await _unitOfWork.SmallCollectionPoints.GetAsync(p =>
                            p.SmallCollectionPointsId == spId && p.CompanyId == request.CompanyId);

                        if (point == null)
                            throw new Exception($"Kho {spId} không tồn tại hoặc không thuộc quản lý của công ty này.");

                        await AddUniqueOffDayAsync(date, request.CompanyId, spId, request.Reason);
                    }
                }
            }

            return await _unitOfWork.SaveAsync() > 0;
        }

        private async Task AddUniqueOffDayAsync(DateOnly date, string? companyId, string? spId, string? reason)
        {
            var exists = await _unitOfWork.CollectionOffDays.GetAsync(x =>
                x.OffDate == date && x.CompanyId == companyId && x.SmallCollectionPointId == spId);

            if (exists == null)
            {
                await _unitOfWork.CollectionOffDays.AddAsync(new CollectionOffDay
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    SmallCollectionPointId = spId,
                    OffDate = date,
                    Reason = reason,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        public async Task<bool> RemoveOffDayAsync(string? companyId, string? pointId, DateOnly date)
        {
            var offDay = await _unitOfWork.CollectionOffDays.GetAsync(x =>
                x.OffDate == date && x.CompanyId == companyId && x.SmallCollectionPointId == pointId);

            if (offDay == null)
                throw new Exception("Không tìm thấy lịch nghỉ yêu cầu để xóa.");

            if (date <= DateOnly.FromDateTime(DateTime.Now))
                throw new Exception("Chỉ có thể hủy lịch nghỉ cho các ngày trong tương lai.");

            _unitOfWork.CollectionOffDays.Delete(offDay);
            return await _unitOfWork.SaveAsync() > 0;
        }

        public async Task<List<CompanyAvailableModel>> GetAvailableCompaniesForAssignAsync(DateOnly workDate)
        {
            var allCompanies = await _unitOfWork.Companies.GetAllAsync(
                c => c.CompanyType == CompanyType.CTY_THU_GOM.ToString() && c.Status == CompanyStatus.DANG_HOAT_DONG.ToString(),
                includeProperties: "SmallCollectionPoints"
            );

            var offDays = await _unitOfWork.CollectionOffDays.GetAllAsync(x => x.OffDate == workDate);

            var offCompanyIds = offDays.Where(x => x.CompanyId != null && x.SmallCollectionPointId == null)
                                       .Select(x => x.CompanyId).ToList();

            var offPointIds = offDays.Where(x => x.SmallCollectionPointId != null)
                                     .Select(x => x.SmallCollectionPointId).ToList();

            return allCompanies
                .Where(c => !offCompanyIds.Contains(c.CompanyId))
                .Select(c => new CompanyAvailableModel
                {
                    CompanyId = c.CompanyId,
                    CompanyName = c.Name,
                    ActivePoints = c.SmallCollectionPoints
                        .Where(p => !offPointIds.Contains(p.SmallCollectionPointsId) &&
                        p.Status == SmallCollectionPointStatus.DANG_HOAT_DONG.ToString())
                        .Select(p => p.Name).ToList()
                })
                .Where(c => c.ActivePoints.Any())
                .ToList();
        }

        public async Task<PagedResult<CollectionOffDayModel>> GetAllOffDaysAsync(
            string? companyId,
            DateOnly? date,
            string? smallCollectionPointId,
            int page = 1,
            int limit = 10)
        {
            List<string> relatedPointIds = new List<string>();
            if (!string.IsNullOrEmpty(companyId))
            {
                var points = await _unitOfWork.SmallCollectionPoints.GetAllAsync(p => p.CompanyId == companyId);
                relatedPointIds = points.Select(p => p.SmallCollectionPointsId).ToList();
            }

            var query = _unitOfWork.CollectionOffDays.GetQueryable();

            if (!string.IsNullOrEmpty(companyId))
            {
                query = query.Where(x => x.CompanyId == companyId
                                         || (x.SmallCollectionPointId != null && relatedPointIds.Contains(x.SmallCollectionPointId)));
            }

            if (!string.IsNullOrEmpty(smallCollectionPointId))
            {
                query = query.Where(x => x.SmallCollectionPointId == smallCollectionPointId);
            }

            if (date.HasValue)
            {
                query = query.Where(x => x.OffDate == date.Value);
            }

            var totalItems = await query.CountAsync();

            var offDays = await _unitOfWork.CollectionOffDays.GetAllAsync(
                filter: x => query.Select(q => q.Id).Contains(x.Id),
                includeProperties: "Company,SmallCollectionPoint"
            );

            var resultItems = offDays
                .Select(x => new CollectionOffDayModel
                {
                    Id = x.Id,
                    CompanyId = x.CompanyId ?? x.SmallCollectionPoint?.CompanyId,
                    CompanyName = x.Company?.Name ?? "N/A",
                    SmallCollectionPointId = x.SmallCollectionPointId,
                    PointName = x.SmallCollectionPoint?.Name ?? "Nghỉ toàn hệ thống công ty",
                    OffDate = x.OffDate,
                    Reason = x.Reason
                })
                .OrderByDescending(x => x.OffDate)
                .Skip((page - 1) * limit) 
                .Take(limit)              
                .ToList();

            return new PagedResult<CollectionOffDayModel>
            {
                Data = resultItems,
                TotalItems = totalItems,
                Page = page,
                Limit = limit
            };
        }
    }
}