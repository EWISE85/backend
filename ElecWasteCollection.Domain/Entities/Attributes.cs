using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
    public enum AttributeStatus
    {
        [Description("Đang hoạt động")]
        DANG_HOAT_DONG,

        [Description("Không hoạt động")]
        KHONG_HOAT_DONG
    }
    public class Attributes
	{
		public Guid AttributeId { get; set; }

		public string Name { get; set; }

		public string Status { get; set; } = AttributeStatus.DANG_HOAT_DONG.ToString();

		public virtual ICollection<AttributeOptions> AttributeOptions { get; set; }

		public virtual ICollection<CategoryAttributes> CategoryAttributes { get; set; }

		public virtual ICollection<ProductValues> ProductValues { get; set; }

	}
}
