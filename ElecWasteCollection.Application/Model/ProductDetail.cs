using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class ProductDetail
	{
		public Guid ProductId { get; set; }

		public Guid CategoryId { get; set; }

		public string CategoryName { get; set; }
		public string Description { get; set; } // Mô tả SẢN PHẨM (ví dụ: "Tivi hỏng màn")
												//public string Condition { get; set; } // Tình trạng (từ Product.Condition)

		public Guid BrandId { get; set; } // Id thương hiệu
		public string BrandName { get; set; } // Tên thương hiệu (ví dụ: "Samsung")

		public List<string> ProductImages { get; set; } // Danh sách ảnh sản phẩm
		public string Status { get; set; } // Trạng thái hiện tại của sản phẩm (ví dụ: "Chờ thu gom", "Đã nhập kho", "Hủy")
		public double? EstimatePoint { get; set; } // Điểm ước tính dựa trên kích thước

		public double? RealPoints { get; set; } // Điểm thực tế sau khi kiểm tra sản phẩm

		public string? ChangedPointMessage { get; set; } // Lý do thay đổi điểm (nếu có)	

		public string? RejectMessage { get; set; } // Lý do hủy sản phẩm (nếu có)
		public UserResponse Sender { get; set; }

		public CollectorResponse? Collector { get; set; }
		public Guid? CollectionRouterId { get; set; }
		public DateOnly? PickUpDate { get; set; }
		public TimeOnly? EstimatedTime { get; set; }
		public string Address { get; set; }

		public string? QRCode { get; set; } // Mã QR của sản phẩm

		public bool IsChecked { get; set; } // Đã được kiểm tra bởi nhân viên thu gom hay chưa

		public List<DailyTimeSlots> Schedule { get; set; }


		public List<ProductValueDetailModel>? Attributes { get; set; } // (ví dụ: "Nặng: 55 kg")
	}
}
