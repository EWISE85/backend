using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public enum RoleStatus
	{
		[Description("Đang hoạt động")]
		DANG_HOAT_DONG,

		[Description("Không hoạt động")]
		KHONG_HOAT_DONG,

		[Description("Bị đình chỉ")]
		BI_DINH_CHI
	}
	public enum UserRole
	{
		AdminWarehouse,
		Collector,
		User,
		Admin,
		//AdminCompany,
		//Shipper,
		RecyclingCompany
	}
	public class Role
	{
		public Guid RoleId { get; set; }
		public string Name { get; set; }

		public string Status { get; set; }

		public virtual ICollection<User> Users { get; set; } = new List<User>();
	}
}
