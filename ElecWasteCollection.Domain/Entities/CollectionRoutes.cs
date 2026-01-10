using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public enum CollectionRouteStatus
	{
		[Description("Chưa bắt đầu")]
		CHUA_BAT_DAU,
		[Description("Đang tiến hành")]
		DANG_TIEN_HANH,
		[Description("Hoàn thành")]
		HOAN_THANH,
		[Description("Hủy bỏ")]
		HUY_BO
	}
	public class CollectionRoutes
	{
		public Guid CollectionRouteId { get; set; }

		public Guid ProductId { get; set; }

		public int CollectionGroupId { get; set; }

		public DateOnly CollectionDate { get; set; }
		public TimeOnly EstimatedTime { get; set; }
		public TimeOnly? Actual_Time { get; set; }

		public List<string>? ConfirmImages { get; set; }


		public string? RejectMessage { get; set; }
        public double DistanceKm { get; set; }

        public string Status { get; set; }

		public Products Product { get; set; }

		public CollectionGroups CollectionGroup { get; set; }
	}
}
