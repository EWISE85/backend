using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public enum CategoryAttributeStatus
	{
		[Description("Hoạt động")]
		HOAT_DONG,
		[Description("Không hoạt động")]
		KHONG_HOAT_DONG
	}
	public class CategoryAttributes
	{
		public Guid CategoryAttributeId { get; set; }

		public Guid CategoryId { get; set; }

		public Guid AttributeId { get; set; }

		public double? MinValue { get; set; }

		public double? MaxValue { get; set; }

		public string Unit { get; set; }

		public string Status { get; set; }

		public Category Category { get; set; }

		public Attributes Attribute { get; set; }
	}
}
