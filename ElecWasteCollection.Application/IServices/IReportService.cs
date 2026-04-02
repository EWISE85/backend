using ElecWasteCollection.Application.Model;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IReportService
	{
		Task<bool> CreateReport(CreateReportModel createReportModel);

		Task<bool> AnswerReport(Guid reportId, string answerMessage);

		Task<PagedResultModel<ReportModel>> GetPagedReport (UserReportQueryModel model);
		Task<PagedResultModel<ReportModel>> GetPagedReportForUser(UserReportQueryModel model);

		Task<List<string>> GetReportTypes();

		Task<ReportModel> GetReport(Guid reportId);

	}
}
