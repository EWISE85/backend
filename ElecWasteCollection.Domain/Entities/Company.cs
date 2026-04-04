using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
    public enum CompanyStatus
    {
        [Description("Đang hoạt động")]
        DANG_HOAT_DONG,

        [Description("Không hoạt động")]
        KHONG_HOAT_DONG
    }
    public enum CompanyType
    {
        //[Description("Công ty thu gom")]
        //CTY_THU_GOM,

        [Description("Công ty tái chế")]
        CTY_TAI_CHE
    }
    public class Company
    {
        public string CompanyId { get; set; }
        public string Name { get; set; } = null!;
        public string CompanyEmail { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;

        public string CompanyType { get; set; } = null!;
		public string Status { get; set; } = null!;
        public DateTime Created_At { get; set; }
        public DateTime Updated_At { get; set; }
        public int Priority { get; set; } = 1;
        public bool IsFallback { get; set; } = false;
        public virtual ICollection<User> Users { get; set; } = new List<User>();
		public virtual ICollection<CollectionUnit> CollectionUnits { get; set; } = new List<CollectionUnit>();
		public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
        public virtual ICollection<SystemConfig> CustomSettings { get; set; } = new List<SystemConfig>();
		public virtual ICollection<CompanyRecyclingCategory> CompanyRecyclingCategories { get; set; } = new List<CompanyRecyclingCategory>();
	}
}
