using DocumentFormat.OpenXml.Drawing.Charts;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using ElecWasteCollection.Infrastructure.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ElecWasteCollectionDbContext _context;

        public IGenericRepository<User> Users { get; }
        public IGenericRepository<Company> Companies { get; }
        public IGenericRepository<Products> Products { get; }
        public IGenericRepository<Post> Posts { get; }
        public IGenericRepository<UserAddress> UserAddresses { get; }
        public IGenericRepository<Category> Categories { get; }
        public IGenericRepository<Brand> Brands { get; }
        public IGenericRepository<ProductValues> ProductValues { get; }
        public IGenericRepository<AttributeOptions> AttributeOptions { get; }
        public IGenericRepository<Attributes> Attributes { get; }
		public IGenericRepository<Account> Accounts { get; }
		public IGenericRepository<CollectionGroups> CollectionGroups { get; }

		public IGenericRepository<CategoryAttributes> CategoryAttributes { get; }

		public IGenericRepository<CollectionRoutes> CollecctionRoutes { get; }

		public IGenericRepository<Packages> Packages { get; }

		public IGenericRepository<PointTransactions> PointTransactions { get; }

		public IGenericRepository<ProductImages> ProductImages { get; }

		public IGenericRepository<ProductStatusHistory> ProductStatusHistory { get; }

		public IGenericRepository<Shifts> Shifts { get; }

		public IGenericRepository<SmallCollectionPoints> SmallCollectionPoints { get; }

		public IGenericRepository<UserPoints> UserPoints { get; }

		public IGenericRepository<Vehicles> Vehicles { get; }

		public IGenericRepository<ForgotPassword> ForgotPasswords { get; }

		public IGenericRepository<SystemConfig> SystemConfig { get; }

		public IGenericRepository<UserDeviceToken> UserDeviceTokens { get; }

		public IGenericRepository<Notifications> Notifications { get; }
        public UnitOfWork(ElecWasteCollectionDbContext context)
        {
            _context = context;

            Users = new GenericRepository<User>(_context);
            Companies = new GenericRepository<Company>(_context);
            Products = new GenericRepository<Products>(_context);
            Posts = new GenericRepository<Post>(_context);
            UserAddresses = new GenericRepository<UserAddress>(_context);
            Categories = new GenericRepository<Category>(_context);
            Brands = new GenericRepository<Brand>(_context);
            ProductValues = new GenericRepository<ProductValues>(_context);
            AttributeOptions = new GenericRepository<AttributeOptions>(_context);
            Attributes = new GenericRepository<Attributes>(_context);
			Accounts = new GenericRepository<Account>(_context);
			CategoryAttributes = new GenericRepository<CategoryAttributes>(_context);
			CollecctionRoutes = new GenericRepository<CollectionRoutes>(_context);
			Packages = new GenericRepository<Packages>(_context);
			PointTransactions = new GenericRepository<PointTransactions>(_context);
			ProductImages = new GenericRepository<ProductImages>(_context);
			ProductStatusHistory = new GenericRepository<ProductStatusHistory>(_context);
			Shifts = new GenericRepository<Shifts>(_context);
			SmallCollectionPoints = new GenericRepository<SmallCollectionPoints>(_context);
			UserPoints = new GenericRepository<UserPoints>(_context);
			Vehicles = new GenericRepository<Vehicles>(_context);
			ForgotPasswords = new GenericRepository<ForgotPassword>(_context);
			SystemConfig = new GenericRepository<SystemConfig>(_context);
			CollectionGroups = new GenericRepository<CollectionGroups>(_context);
			UserDeviceTokens = new GenericRepository<UserDeviceToken>(_context);
			Notifications = new GenericRepository<Notifications>(_context);

        }

		public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
