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
        public IGenericRepository<CollectionCompany> CollectionCompanies { get; }
        public IGenericRepository<Products> Products { get; }
        public IGenericRepository<Post> Posts { get; }
        public IGenericRepository<UserAddress> UserAddresses { get; }
        public IGenericRepository<Category> Categories { get; }
        public IGenericRepository<Brand> Brands { get; }
        public IGenericRepository<ProductValues> ProductValues { get; }
        public IGenericRepository<AttributeOptions> AttributeOptions { get; }
        public IGenericRepository<Attributes> Attributes { get; }
        public IGenericRepository<SmallCollectionPoints> SmallCollectionPoints { get; }
        public IGenericRepository<Vehicles> Vehicles { get; }
        public IGenericRepository<Shifts> Shifts { get; }
        public IGenericRepository<CollectionGroups> CollectionGroups { get; }
        public IGenericRepository<CollectionRoutes> CollectionRoutes { get; }
        public IGenericRepository<ProductStatusHistory> ProductStatusHistories { get; }




        public UnitOfWork(ElecWasteCollectionDbContext context)
        {
            _context = context;

            Users = new GenericRepository<User>(_context);
            CollectionCompanies = new GenericRepository<CollectionCompany>(_context);
            Products = new GenericRepository<Products>(_context);
            Posts = new GenericRepository<Post>(_context);
            UserAddresses = new GenericRepository<UserAddress>(_context);
            Categories = new GenericRepository<Category>(_context);
            Brands = new GenericRepository<Brand>(_context);
            ProductValues = new GenericRepository<ProductValues>(_context);
            AttributeOptions = new GenericRepository<AttributeOptions>(_context);
            Attributes = new GenericRepository<Attributes>(_context);
            SmallCollectionPoints = new GenericRepository<SmallCollectionPoints>(_context);
            Vehicles = new GenericRepository<Vehicles>(_context);
            Shifts = new GenericRepository<Shifts>(_context);
            CollectionGroups = new GenericRepository<CollectionGroups>(_context);
            CollectionRoutes = new GenericRepository<CollectionRoutes>(_context);
            ProductStatusHistories = new GenericRepository<ProductStatusHistory>(_context);
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
