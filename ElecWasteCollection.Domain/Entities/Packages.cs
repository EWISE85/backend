using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public enum PackageStatus
	{
		[Description("Đang đóng gói")]
		DANG_DONG_GOI,
		[Description("Đã đóng thùng")]
		DA_DONG_THUNG,
		[Description("Đang vận chuyển")]
		DANG_VAN_CHUYEN,
		[Description("Tái chế")]
		TAI_CHE
	}
	public class Packages
	{
		public string PackageId { get; set; }
		public DateTime CreateAt { get; set; }
		public string SmallCollectionPointsId { get; set; }
		public string Status { get; set; }
        public SmallCollectionPoints SmallCollectionPoints { get; set; }
		public ICollection<Products> Products { get; set; }
	}
}
