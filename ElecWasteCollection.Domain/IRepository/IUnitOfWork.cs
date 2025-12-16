using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<User> Users { get; }
        IGenericRepository<CollectionCompany> CollectionCompanies { get; }
        IGenericRepository<Products> Products { get; }
        IGenericRepository<Post> Posts { get; }
        IGenericRepository<UserAddress> UserAddresses { get; }
        IGenericRepository<Category> Categories { get; }
        IGenericRepository<Brand> Brands { get; }
        IGenericRepository<ProductValues> ProductValues { get; }
        IGenericRepository<AttributeOptions> AttributeOptions { get; }
        IGenericRepository<Attributes> Attributes { get; }

        IGenericRepository<SmallCollectionPoints> SmallCollectionPoints { get; }
        IGenericRepository<Vehicles> Vehicles { get; }
        IGenericRepository<Shifts> Shifts { get; }
        IGenericRepository<CollectionGroups> CollectionGroups { get; }
        IGenericRepository<CollectionRoutes> CollectionRoutes { get; }
        IGenericRepository<ProductStatusHistory> ProductStatusHistories { get; }
        Task<int> SaveAsync();
    }
}
