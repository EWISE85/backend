using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class ReportService : IReportService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IReportRepository _reportRepository;

		public ReportService(IUnitOfWork unitOfWork, IReportRepository reportRepository)
		{
			_unitOfWork = unitOfWork;
			_reportRepository = reportRepository;
		}

		public async Task<bool> AnswerReport(Guid reportId, string answerMessage)
		{
			var report = await _unitOfWork.UserReports.GetByIdAsync(reportId);
			if (report == null) throw new AppException("Không tìm thấy khiếu nại",400);
			if(report.Status != ReportStatus.DANG_XU_LY.ToString()) throw new AppException("Chỉ có thể trả lời khiếu nại đang xử lý", 400);
			report.ResolveMessage = answerMessage;
			report.ResolvedAt = DateTime.UtcNow;
			_unitOfWork.UserReports.Update(report);
			return await _unitOfWork.SaveAsync() > 0;

		}

		public async Task<bool> CreateReport(CreateReportModel createReportModel)
		{
			var newReport = new UserReport
			{
				UserReportId = Guid.NewGuid(),
				UserId = createReportModel.UserId,
				CollectionRouteId = createReportModel.CollectionRouteId,
				Description = createReportModel.Description,
				ReportType = createReportModel.ReportType,
				CreatedAt = DateTime.UtcNow,
				Status = ReportStatus.DANG_XU_LY.ToString()
			};
			await _unitOfWork.UserReports.AddAsync(newReport);
			return await _unitOfWork.SaveAsync() > 0;
		}

		public async Task<PagedResultModel<ReportModel>> GetPagedReport(UserReportQueryModel model)
		{
			string typeEnum = null;
			if (!string.IsNullOrEmpty(model.Type))
			{
				typeEnum = StatusEnumHelper.GetValueFromDescription<ReportType>(model.Type).ToString();
			}
			string statusEnum = null;
			if (!string.IsNullOrEmpty(model.Status))
			{
				statusEnum = StatusEnumHelper.GetValueFromDescription<ReportStatus>(model.Status).ToString();
			}
			var (reports, totalCount) = await _reportRepository.GetPagedReport(typeEnum,statusEnum,model.Start,model.End,model.PageNumber,model.Limit);
			var reportModels = reports.Select(r => new ReportModel
			{
				ReportId = r.UserReportId,
				ReportUserId = r.UserId,
				ReportRouteId = r.CollectionRouteId,
				ReportDescription = r.Description,
				ReportType = StatusEnumHelper.ConvertDbCodeToVietnameseName<ReportType>(r.ReportType),
				CreatedAt = r.CreatedAt,
				ResolvedAt = r.ResolvedAt,
				AnswerMessage = r.ResolveMessage,
				ReportUserName = r.User.Name,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ReportStatus>(r.Status)
			}).ToList();
			return new PagedResultModel<ReportModel>(
				reportModels,
				model.PageNumber,
				model.Limit,
				totalCount
			);
		}

		public async Task<PagedResultModel<ReportModel>> GetPagedReportForUser(UserReportQueryModel model)
		{
			
				string typeEnum = null;
				if (!string.IsNullOrEmpty(model.Type))
				{
					typeEnum = StatusEnumHelper.GetValueFromDescription<ReportType>(model.Type).ToString();
				}
				string statusEnum = null;
				if (!string.IsNullOrEmpty(model.Status))
				{
					statusEnum = StatusEnumHelper.GetValueFromDescription<ReportStatus>(model.Status).ToString();
				}
				var (reports, totalCount) = await _reportRepository.GetPagedReport(typeEnum, statusEnum, model.Start, model.End, model.PageNumber, model.Limit);
				var reportModels = reports.Select(r => new ReportModel
				{
					ReportId = r.UserReportId,
					ReportUserId = r.UserId,
					ReportRouteId = r.CollectionRouteId,
					ReportDescription = r.Description,
					ReportType = StatusEnumHelper.ConvertDbCodeToVietnameseName<ReportType>(r.ReportType),
					CreatedAt = r.CreatedAt,
					ResolvedAt = r.ResolvedAt,
					AnswerMessage = r.ResolveMessage,
					ReportUserName = r.User.Name,
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ReportStatus>(r.Status)
				}).ToList();
				return new PagedResultModel<ReportModel>(
					reportModels,
					model.PageNumber,
					model.Limit,
					totalCount
				);
			
		}

		public async Task<List<string>> GetReportTypes()
		{
			var list = Enum.GetValues(typeof(ReportType))
				.Cast<ReportType>()
				.Select(e =>
				{
					var field = typeof(ReportType).GetField(e.ToString());
					var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
										 .FirstOrDefault() as DescriptionAttribute;

					return attribute?.Description ?? e.ToString();
				})
				.ToList();

			return await Task.FromResult(list);
		}
	}
}
