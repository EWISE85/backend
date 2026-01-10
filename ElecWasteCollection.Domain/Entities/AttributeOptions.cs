using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
    public enum AttributeOptionStatus
    {
        [Description("Đang hoạt động")]
        DANG_HOAT_DONG,

        [Description("Không hoạt động")]
        KHONG_HOAT_DONG
    }
    public class AttributeOptions
	{
		public Guid OptionId { get; set; }

		public Guid AttributeId { get; set; }

		public string OptionName { get; set; }

		public double? EstimateWeight { get; set; } // (kg)

		public double? EstimateVolume { get; set; } // (m³)

		public string Status { get; set; } = AttributeOptionStatus.DANG_HOAT_DONG.ToString();

		public Attributes Attribute { get; set; }
	}
}
