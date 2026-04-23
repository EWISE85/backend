using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public enum ReportType
	{
		[Description("Lỗi hệ thống")]
		LOI_HE_THONG,
		[Description("Vấn đề thu gom")]
		LOI_THU_GOM,
		[Description("Lỗi điểm thu gom")]
		LOI_DIEM
	}
	public enum ReportStatus
	{
		//PENDING,
		[Description("Đang xử lý")]
		DANG_XU_LY,
		[Description("Đã xử lý")]
		DA_XU_LY,
		//REJECTED
	}
	public class UserReport
	{
		public Guid UserReportId { get; set; } = Guid.NewGuid();

		public Guid UserId { get; set; }

		public Guid? CollectionRouteId { get; set; }

		public string Description { get; set; } = string.Empty;

		public string ReportType { get; set; } = string.Empty; // VD: SYSTEM_BUG, COLLECTION_ISSUE, ILLEGAL_DUMP

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public DateTime? ResolvedAt { get; set; }

		public string? ResolveMessage { get; set; }
		public List<string>? Images { get; set; }


		public string Status { get; set; }

		public User User { get; set; }

		public CollectionRoutes? CollectionRoute { get; set; }
	}
}
