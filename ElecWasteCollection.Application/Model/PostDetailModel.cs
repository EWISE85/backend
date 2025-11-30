using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class PostDetailModel
	{
		public Guid Id { get; set; }
		public User Sender { get; set; } // Gửi đầy đủ thông tin người đăng

		public string ParentCategory { get; set; } // Tên Parent Category

		public string SubCategory { get; set; } // Tên Category
		public string Status { get; set; }

		public List<string>? CheckMessage { get; set; }
		public string? RejectMessage { get; set; }
		public DateTime Date { get; set; }
		public string Address { get; set; }

		public List<DailyTimeSlots> Schedule { get; set; }

		// 2. Ghi chú của bài đăng (từ Post.Description)
		public string PostNote { get; set; }

		// 3. Toàn bộ ảnh VÀ các nhãn AI
		public List<string> ImageUrls { get; set; }

		// 2. Danh sách tag đã được "GỘP LẠI"
		public List<LabelModel> AggregatedAiLabels { get; set; }

		public double EstimatePoint { get; set; }

		// 4. Thông tin chi tiết của chính sản phẩm
		public ProductDetailModel Product { get; set; }
	}
	public class ProductDetailModel
	{
		public Guid ProductId { get; set; }
		public string Description { get; set; } // Mô tả SẢN PHẨM (ví dụ: "Tivi hỏng màn")

		public Guid CategoryId { get; set; } // Id danh mục sản phẩm

		public string CategoryName { get; set; } // Tên danh mục sản phẩm (ví dụ: "Tivi")
		public Guid BrandId { get; set; } // Id thương hiệu
		public string BrandName { get; set; } // Tên thương hiệu (ví dụ: "Samsung")

		public string? QrCode { get; set; } // Mã QR của sản phẩm
		public string Status { get; set; } // Trạng thái hiện tại của sản phẩm (ví dụ: "Chờ thu gom", "Đã nhập kho", "Hủy")

		// Thông tin kích thước (1 trong 2 sẽ có)

		public bool IsChecked { get; set; }
		public List<ProductValueDetailModel>? Attributes { get; set; } // (ví dụ: "Nặng: 55 kg")
	}

	public class ProductValueDetailModel
	{
		public Guid AttributeId { get; set; } // Id của thuộc tính (ví dụ: "Trọng lượng")
		public string AttributeName { get; set; } // "Trọng lượng"

		public Guid? OptionId { get; set; }

		public string? OptionName { get; set; }

		public string? Value { get; set; } // "55"
	}
}
