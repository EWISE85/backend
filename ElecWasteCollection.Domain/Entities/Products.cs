using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public enum ProductStatus
	{
		[Description("Chờ duyệt")]
		CHO_DUYET,
		[Description("Đã hủy")]
		DA_HUY,
		[Description("Đã từ chối")]
		DA_TU_CHOI,
		[Description("Chờ phân kho")]
		CHO_PHAN_KHO,
        [Description("Không tìm thấy điểm thu gom")]
        KHONG_TIM_THAY_DIEM_THU_GOM,
        [Description("Chờ gom nhóm")]
		CHO_GOM_NHOM,
		[Description("Chờ thu gom")]
		CHO_THU_GOM,
		[Description("Đã thu gom")]
		DA_THU_GOM,
		[Description("Hủy bỏ")]
		HUY_BO,
		[Description("Nhập kho")]
		NHAP_KHO,
		[Description("Đã đóng thùng")]
		DA_DONG_THUNG,
		[Description("Đang vận chuyển")]
		DANG_VAN_CHUYEN,
		[Description("Tái chế")]
		TAI_CHE
	}
	public class Products
	{
		public Guid ProductId { get; set; }
		public Guid CategoryId { get; set; }
		public Guid BrandId { get; set; }

		public Guid UserId { get; set; }

		public string? PackageId { get; set; }


		public string Description { get; set; }

		public DateOnly? CreateAt { get; set; }

		public string? QRCode { get; set; }

		public string Status { get; set; }

		public bool isChecked { get; set; } = false;

		public string? SmallCollectionPointId { get; set; }
        public DateOnly? AssignedAt { get; set; }
        public Category Category { get; set; }

		public Brand Brand { get; set; }

		public Packages? Package { get; set; }

		public User User { get; set; }

		public SmallCollectionPoints? SmallCollectionPoint { get; set; }

		public virtual ICollection<ProductImages> ProductImages { get; set; } = new List<ProductImages>();

		public virtual ICollection<ProductValues> ProductValues { get; set; } = new List<ProductValues>();
		public virtual ICollection<ProductStatusHistory> ProductStatusHistories { get; set; } = new List<ProductStatusHistory>();

		public virtual ICollection<CollectionRoutes> CollectionRoutes { get; set; } = new List<CollectionRoutes>();

		public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

		public virtual ICollection<PointTransactions> PointTransactions { get; set; } = new List<PointTransactions>();
	}
}
