using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public enum VoucherStatus
	{
		[Description("Hoạt động")]
		HOAT_DONG,
		[Description("Hết hạn")]
		HET_HAN,
		[Description("Không hoạt động")]
		KHONG_HOAT_DONG
	}
	public class Voucher
	{
		public Guid VoucherId { get; set; }

		public string Code { get; set; }

		public string Name { get; set; }

		public string? ImageUrl { get; set; }

		public string Description { get; set; }

		public DateOnly StartAt { get; set; }

		public DateOnly EndAt { get; set; }

		public double Value { get; set; }

		public double PointsToRedeem { get; set; }

		public int Quantity { get; set; }

		public string Status { get; set; }

		public virtual ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();
		public virtual ICollection<PointTransactions> PointTransactions { get; set; } = new List<PointTransactions>();
	}
}
