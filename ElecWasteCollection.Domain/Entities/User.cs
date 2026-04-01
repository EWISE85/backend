using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public enum UserRole
	{
		AdminWarehouse,
		Collector,
		User,
		Admin,
		AdminCompany,
		Shipper,
		Recycler
	}
    public enum UserStatus
    {
        [Description("Đang hoạt động")]
        DANG_HOAT_DONG,     

        [Description("Không hoạt động")]
        KHONG_HOAT_DONG,

        [Description("Bị đình chỉ")]
        BI_DINH_CHI 
    }
    public class User
	{
		public Guid UserId { get; set; }

		public string? AppleId { get; set; }
		public string? Name { get; set; }

		public string? Email { get; set; }

		public string? Phone { get; set; }

		public string? Avatar { get; set; }

		public string Role { get; set; }

		public string? SmallCollectionPointId { get; set; }

		public string? CollectionCompanyId { get; set; }

		public string? CollectorCode { get; set; }

		public double Points { get; set; }

		public DateTime CreateAt { get; set; }

		public double TotalCo2Saved { get; set; } = 0.0;
		public Guid? CurrentRankId { get; set; }

		public string Status { get; set; }

		public Company? CollectionCompany { get; set; }

		public SmallCollectionPoints? SmallCollectionPoint { get; set; }

		public Rank Rank { get; set; }

		public virtual ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();

		public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();


		public virtual ICollection<Products> Products { get; set; } = new List<Products>();

		public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

		public virtual ICollection<PointTransactions> PointTransactions { get; set; } = new List<PointTransactions>();

		public virtual ICollection<Shifts> Shifts { get; set; } = new List<Shifts>();

		public virtual ICollection<ForgotPassword> ForgotPasswords { get; set; } = new List<ForgotPassword>();

		public virtual ICollection<UserDeviceToken> UserDeviceTokens { get; set; } = new List<UserDeviceToken>();

		public virtual ICollection<Notifications> Notifications { get; set; } = new List<Notifications>();

		public virtual ICollection<UserVoucher> UserVouchers { get; set; } = new List<UserVoucher>();

		public virtual ICollection<UserReport> UserReports { get; set; } = new List<UserReport>();

	}
}
