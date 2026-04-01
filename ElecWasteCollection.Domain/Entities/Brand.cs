using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public enum BrandStatus
	{
		[Description("Hoạt động")]
		HOAT_DONG,
		[Description("Không hoạt động")]
		KHONG_HOAT_DONG
	}
	public class Brand
	{
		public Guid BrandId { get; set; }

		public string Name { get; set; }

		public string Status { get; set; } = BrandStatus.HOAT_DONG.ToString();

		public virtual ICollection<Products> Products { get; set; } = new List<Products>();
		public virtual ICollection<BrandCategory> BrandCategories { get; set; } = new List<BrandCategory>();

	}
}
