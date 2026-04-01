using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class BrandCategoryMapModel
	{
		public string CategoryName { get; set; } // Tên danh mục từ Excel (vd: Tivi, Laptop)

		public Guid CategoryId { get; set; }

		public Guid BrandId { get; set; }        // Id thương hiệu đã tồn tại trong hệ thống (được lấy từ DB sau khi check và update thương hiệu)
		public string BrandName { get; set; }    // Tên thương hiệu từ Excel (vd: Samsung, Dell)
		public double Points { get; set; }       // Số điểm thưởng khi thu gom cặp này

		public string Status { get; set; }          // Trạng thái hoạt động của cặp này (vd: Hoạt động, Không hoạt động)
	}
}
