using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class CategoryAttributeMapModel
	{
		public string CategoryName { get; set; }  // Tên danh mục (vd: Tủ lạnh)
		public string AttributeName { get; set; } // Tên thuộc tính (vd: Dung tích, Trọng lượng)
		public string Unit { get; set; }          // Đơn vị đo (vd: Lít, Kg, Inch)

		public double? MinValue { get; set; }
		public double? MaxValue { get; set; }

	}
}
