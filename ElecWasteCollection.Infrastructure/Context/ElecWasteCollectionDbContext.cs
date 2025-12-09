using ElecWasteCollection.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.Context
{
	public class ElecWasteCollectionDbContext : DbContext
	{
		public ElecWasteCollectionDbContext(DbContextOptions<ElecWasteCollectionDbContext> options) : base(options)
		{
		}
		public DbSet<User> Users { get; set; }
		public DbSet<Account> Accounts { get; set; }
		public DbSet<Products> Products { get; set; }
		public DbSet<ProductImages> ProductImages { get; set; }
		public DbSet<Category> Categories { get; set; }
		public DbSet<Brand> Brands { get; set; }
		public DbSet<Attributes> Attributes { get; set; }
		public DbSet<AttributeOptions> AttributeOptions { get; set; }
		public DbSet<CategoryAttributes> CategoryAttributes { get; set; }
		public DbSet<CollectionCompany> CollectionCompanies { get; set; }
		public DbSet<SmallCollectionPoints> SmallCollectionPoints { get; set; }
		public DbSet<CollectionGroups> CollectionGroups { get; set; }
		public DbSet<CollectionRoutes> CollectionRoutes { get; set; }
		public DbSet<Packages> Packages { get; set; }
		public DbSet<PointTransactions> PointTransactions { get; set; }
		public DbSet<Post> Posts { get; set; }
		public DbSet<ProductStatusHistory> ProductStatusHistories { get; set; }
		public DbSet<ProductValues> ProductValues { get; set; }
		public DbSet<Shifts> Shifts { get; set; }
		public DbSet<UserAddress> UserAddresses { get; set; }
		public DbSet<UserPoints> UserPoints { get; set; }
		public DbSet<Vehicles> Vehicles { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<CollectionCompany>(entity =>
			{
				entity.ToTable("CollectionCompany");
				entity.HasKey(e => e.CollectionCompanyId);
				entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
				entity.HasIndex(e => e.Name).IsUnique();
			});

			modelBuilder.Entity<SmallCollectionPoints>(entity =>
			{
				entity.ToTable("SmallCollectionPoints");
				entity.HasKey(e => e.SmallCollectionPointsId);
				entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
				entity.HasIndex(e => e.Name).IsUnique();

				entity.HasOne(e => e.CollectionCompany)
					  .WithMany(c => c.SmallCollectionPoints)
					  .HasForeignKey(e => e.CompanyId)
					  .HasConstraintName("FK_SmallCollectionPoints_CollectionCompany");
			});

			modelBuilder.Entity<User>(entity =>
			{
				entity.ToTable("User");
				entity.HasKey(e => e.UserId);
				entity.Property(e => e.UserId).ValueGeneratedOnAdd();
				entity.Property(e => e.CollectionCompanyId).IsRequired(false);
				entity.Property(e => e.SmallCollectionPointId).IsRequired(false);

				entity.HasOne(e => e.CollectionCompany)
					  .WithMany(c => c.Users)
					  .HasForeignKey(e => e.CollectionCompanyId)
					  .HasConstraintName("FK_User_CollectionCompany");

				entity.HasOne(e => e.SmallCollectionPoint)
				.WithMany(s => s.Users)
					  .HasForeignKey(e => e.SmallCollectionPointId)
					  .HasConstraintName("FK_User_SmallCollectionPoints");
			});

			modelBuilder.Entity<Account>(entity =>
			{
				entity.ToTable("Account");
				entity.HasKey(e => e.AccountId);
				entity.Property(e => e.AccountId).ValueGeneratedOnAdd();
				entity.Property(e => e.UserId).IsRequired();
				entity.HasOne(e => e.User)
					  .WithMany(u => u.Accounts)
					  .HasForeignKey(e => e.UserId)
					  .HasConstraintName("FK_Account_User");
			});

			modelBuilder.Entity<UserPoints>(entity =>
			{
				entity.ToTable("UserPoints");
				entity.HasKey(e => e.UserPointId);
				entity.Property(e => e.UserPointId).ValueGeneratedOnAdd();
				entity.Property(e => e.UserId).IsRequired();

				entity.HasOne(e => e.User)
					  .WithMany(u => u.UserPoints)
					  .HasForeignKey(e => e.UserId)
					  .HasConstraintName("FK_UserPoints_User");

			});

			modelBuilder.Entity<UserAddress>(entity =>
			{
				entity.ToTable("UserAddress");
				entity.HasKey(e => e.UserAddressId);
				entity.Property(e => e.UserAddressId).ValueGeneratedOnAdd();
				entity.Property(e => e.UserId).IsRequired();
				entity.HasOne(e => e.User)
					  .WithMany(u => u.UserAddresses)
					  .HasForeignKey(e => e.UserId)
					  .HasConstraintName("FK_UserAddress_User");
			});

			modelBuilder.Entity<Category>(entity =>
			{
				entity.ToTable("Category");
				entity.HasKey(e => e.CategoryId);
				entity.Property(e => e.CategoryId).ValueGeneratedOnAdd();
				entity.Property(e => e.Name).IsRequired().HasMaxLength(200);

				entity.HasOne(e => e.ParentCategory)
					  .WithMany(e => e.SubCategories)
			          .HasForeignKey(e => e.ParentCategoryId)
					  .HasConstraintName("FK_Category_ParentCategory");
			});

			modelBuilder.Entity<Brand>(entity =>
			{
				entity.ToTable("Brand");
				entity.HasKey(e => e.BrandId);
				entity.Property(e => e.BrandId).ValueGeneratedOnAdd();
				entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
				entity.HasOne(e => e.Category)
					  .WithMany(c => c.Brands)
					  .HasForeignKey(e => e.CategoryId)
					  .HasConstraintName("FK_Brand_Category");
			});

			modelBuilder.Entity<Attributes>(entity =>
			{
				entity.ToTable("Attributes");
				entity.HasKey(e => e.AttributeId);
				entity.Property(e => e.AttributeId).ValueGeneratedOnAdd();
				entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
			});

			modelBuilder.Entity<CategoryAttributes>(entity =>
			{
				entity.ToTable("CategoryAttributes");
				entity.HasKey(e => new { e.CategoryId, e.AttributeId });
				entity.HasOne(e => e.Category)
					  .WithMany(c => c.CategoryAttributes)
					  .HasForeignKey(e => e.CategoryId)
					  .HasConstraintName("FK_CategoryAttributes_Category");
				entity.HasOne(e => e.Attribute)
				.WithMany(a => a.CategoryAttributes)
					  .HasForeignKey(e => e.AttributeId)
					  .HasConstraintName("FK_CategoryAttributes_Attribute");
			});

			modelBuilder.Entity<AttributeOptions>(entity =>
			{
				entity.ToTable("AttributeOptions");
				entity.HasKey(e => e.OptionId);
				entity.Property(e => e.OptionId).ValueGeneratedOnAdd();
				entity.Property(e => e.OptionName).IsRequired().HasMaxLength(200);
				entity.HasOne(e => e.Attribute)
					  .WithMany(a => a.AttributeOptions)
					  .HasForeignKey(e => e.AttributeId)
					  .HasConstraintName("FK_AttributeOptions_Attribute");
			});


			modelBuilder.Entity<Products>(entity =>
			{
				entity.ToTable("Products");
				entity.HasKey(e => e.ProductId);
				entity.Property(e => e.ProductId).ValueGeneratedOnAdd();
				entity.Property(e => e.UserId).IsRequired();
				entity.Property(e => e.SmallCollectionPointId).IsRequired(false);
				entity.Property(e => e.PackageId).IsRequired(false);

				entity.HasOne(e => e.User)
					  .WithMany(u => u.Products)
					  .HasForeignKey(e => e.UserId)
					  .HasConstraintName("FK_Products_User");

				entity.HasOne(e => e.Category)
				      .WithMany(c => c.Products)
					  .HasForeignKey(e => e.CategoryId)
					  .HasConstraintName("FK_Products_Category");

				entity.HasOne(e => e.Brand)
				.WithMany(b => b.Products)
					  .HasForeignKey(e => e.BrandId)
					  .HasConstraintName("FK_Products_Brand");

				entity.HasOne(e => e.SmallCollectionPoint)
				      .WithMany(c => c.Products)
				      .HasForeignKey(e => e.SmallCollectionPointId)
					  .HasConstraintName("FK_Products_SmallCollectionPoints");

				entity.HasOne(e => e.Package)
					  .WithMany(p => p.Products)
					  .HasForeignKey(e => e.PackageId)
					  .HasConstraintName("FK_Products_Packages");
			});

			modelBuilder.Entity<ProductImages>(entity =>
			{
				entity.ToTable("ProductImages");
				entity.HasKey(e => e.ProductImagesId);
				entity.Property(e => e.ProductImagesId).ValueGeneratedOnAdd();
				entity.Property(e => e.ProductId).IsRequired();
				entity.HasOne(e => e.Product)
					  .WithMany(p => p.ProductImages)
					  .HasForeignKey(e => e.ProductId)
					  .HasConstraintName("FK_ProductImages_Products");
			});

			modelBuilder.Entity<ProductStatusHistory>(entity =>
			{
				entity.ToTable("ProductStatusHistory");
				entity.HasKey(e => e.ProductStatusHistoryId);
				entity.Property(e => e.ProductStatusHistoryId).ValueGeneratedOnAdd();
				entity.Property(e => e.ProductId).IsRequired();
				entity.HasOne(e => e.Product)
					  .WithMany(p => p.ProductStatusHistories)
					  .HasForeignKey(e => e.ProductId)
					  .HasConstraintName("FK_ProductStatusHistory_Products");
			});

			modelBuilder.Entity<ProductValues>(entity =>
			{
				entity.ToTable("ProductValues");
				entity.HasKey(e => e.ProductValuesId);
				entity.Property(e => e.ProductValuesId).ValueGeneratedOnAdd();
				entity.Property(e => e.ProductId).IsRequired();

				entity.HasOne(e => e.Product)
					  .WithMany(p => p.ProductValues)
					  .HasForeignKey(e => e.ProductId)
					  .HasConstraintName("FK_ProductValues_Products");

				entity.HasOne(e => e.Attribute)
					  .WithMany(a => a.ProductValues)
					  .HasForeignKey(e => e.AttributeId)
					  .HasConstraintName("FK_ProductValues_Attribute");

			});

			modelBuilder.Entity<PointTransactions>(entity =>
			{
				entity.ToTable("PointTransactions");
				entity.HasKey(e => e.PointTransactionId);
				entity.Property(e => e.PointTransactionId).ValueGeneratedOnAdd();
				entity.Property(e => e.UserId).IsRequired();
				entity.HasOne(e => e.User)
					  .WithMany(u => u.PointTransactions)
					  .HasForeignKey(e => e.UserId)
					  .HasConstraintName("FK_PointTransactions_User");
				entity.HasOne(e => e.Product)
					  .WithMany(p => p.PointTransactions)
					  .HasForeignKey(e => e.ProductId)
					  .HasConstraintName("FK_PointTransactions_Product");
			});


			modelBuilder.Entity<Post>(entity =>
			{
				entity.ToTable("Post");
				entity.HasKey(e => e.PostId);
				entity.Property(e => e.PostId).ValueGeneratedOnAdd();
				entity.Property(e => e.SenderId).IsRequired();
				entity.Property(e => e.CollectionCompanyId).IsRequired(false);
				entity.Property(e => e.AssignedSmallPointId).IsRequired(false);

				entity.HasOne(e => e.Sender)
					  .WithMany(u => u.Posts)
					  .HasForeignKey(e => e.SenderId)
					  .HasConstraintName("FK_Post_User");

				entity.HasOne(e => e.Product)
					  .WithMany(p => p.Posts)
					  .HasForeignKey(e => e.ProductId)
					  .HasConstraintName("FK_Post_Products");

				entity.HasOne(e => e.CollectionCompany)
				.WithMany(c => c.Posts)
					  .HasForeignKey(e => e.CollectionCompanyId)
					  .HasConstraintName("FK_Post_CollectionCompany");

				entity.HasOne(e => e.AssignedSmallPoint)
				.WithMany(s => s.Posts)
					  .HasForeignKey(e => e.AssignedSmallPointId)
					  .HasConstraintName("FK_Post_SmallCollectionPoints");
			});

			modelBuilder.Entity<Vehicles>(entity =>
			{
				entity.ToTable("Vehicles");
				entity.HasKey(e => e.VehicleId);
				entity.Property(e => e.VehicleId).ValueGeneratedOnAdd();
				entity.Property(e => e.Small_Collection_Point).IsRequired();
				entity.HasOne(e => e.SmallCollectionPoints)
					  .WithMany(s => s.Vehicles)
					  .HasForeignKey(e => e.Small_Collection_Point)
					  .HasConstraintName("FK_Vehicles_SmallCollectionPoints");
			});

			modelBuilder.Entity<Shifts>(entity =>
			{
				entity.ToTable("Shifts");
				entity.HasKey(e => e.ShiftId);
				entity.Property(e => e.Vehicle_Id).IsRequired();

				entity.HasOne(e => e.Collector)
					  .WithMany(u => u.Shifts)
					  .HasForeignKey(e => e.CollectorId)
					  .HasConstraintName("FK_Shifts_User");

				entity.HasOne(e => e.Vehicle)
				.WithMany(u => u.Shifts)
				.HasForeignKey(e => e.Vehicle_Id)
					  .HasConstraintName("FK_Shifts_Vehicles");
			});

			modelBuilder.Entity<CollectionGroups>(entity =>
			{
				entity.ToTable("CollectionGroups");
				entity.HasKey(e => e.CollectionGroupId);
				entity.Property(e => e.Shift_Id).IsRequired();
				entity.HasOne(e => e.Shifts)
					  .WithMany(s => s.CollectionGroups)
					  .HasForeignKey(e => e.Shift_Id)
					  .HasConstraintName("FK_CollectionGroups_Shifts");
			});

			modelBuilder.Entity<CollectionRoutes>(entity =>
			{
				entity.ToTable("CollectionRoutes");
				entity.HasKey(e => e.CollectionRouteId);
				entity.Property(e => e.CollectionRouteId).ValueGeneratedOnAdd();
				entity.Property(e => e.CollectionGroupId).IsRequired();
				entity.Property(e => e.ProductId).IsRequired();

				entity.HasOne(e => e.CollectionGroup)
					  .WithMany(c => c.CollectionRoutes)
					  .HasForeignKey(e => e.CollectionGroupId)
					  .HasConstraintName("FK_CollectionRoutes_CollectionGroups");

				entity.HasOne(e => e.Product)
				.WithMany(p => p.CollectionRoutes)
					  .HasForeignKey(e => e.ProductId)
					  .HasConstraintName("FK_CollectionRoutes_Products");

			});

			modelBuilder.Entity<Packages>(entity =>
			{
				entity.ToTable("Packages");
				entity.HasKey(e => e.PackageId);
				entity.Property(e => e.SmallCollectionPointsId).IsRequired();
				entity.HasOne(e => e.SmallCollectionPoints)
					  .WithMany(s => s.Packages)
					  .HasForeignKey(e => e.SmallCollectionPointsId)
					  .HasConstraintName("FK_Packages_SmallCollectionPoints");
			});
		}
	}
}
