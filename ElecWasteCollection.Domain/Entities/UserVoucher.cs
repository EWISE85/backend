using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class UserVoucher
	{
		public Guid UserVoucherId { get;set; }

		public Guid UserId { get; set; }

		public Guid VoucherId { get; set; }

		public DateTime ReceivedAt { get; set; }

		public bool IsUsed { get; set; }

		public DateTime? UsedAt { get; set; }

		public User User { get; set; } = null!;

		public Voucher Voucher { get; set; } = null!;
	}
}
