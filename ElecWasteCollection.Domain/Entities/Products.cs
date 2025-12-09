using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
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
		public Category Category { get; set; }

		public Brand Brand { get; set; }

		public Packages? Package { get; set; }

		public User User { get; set; }

		public SmallCollectionPoints? SmallCollectionPoint { get; set; }

		public virtual ICollection<ProductImages> ProductImages { get; set; } = new List<ProductImages>();

		public virtual ICollection<ProductValues> ProductValues { get; set; } = new List<ProductValues>();
		public virtual ICollection<UserPoints> UserPoints { get; set; } = new List<UserPoints>();

		public virtual ICollection<ProductStatusHistory> ProductStatusHistories { get; set; } = new List<ProductStatusHistory>();

		public virtual ICollection<CollectionRoutes> CollectionRoutes { get; set; } = new List<CollectionRoutes>();

		public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

		public virtual ICollection<PointTransactions> PointTransactions { get; set; } = new List<PointTransactions>();
	}
}
