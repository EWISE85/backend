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
        IGenericRepository<Company> CollectionCompanies { get; }
        IGenericRepository<Products> Products { get; }
        IGenericRepository<Post> Posts { get; }
        IGenericRepository<UserAddress> UserAddresses { get; }
        IGenericRepository<Category> Categories { get; }
        IGenericRepository<Brand> Brands { get; }
        IGenericRepository<ProductValues> ProductValues { get; }
        IGenericRepository<AttributeOptions> AttributeOptions { get; }
        IGenericRepository<Attributes> Attributes { get; }
        IGenericRepository<Account> Accounts { get; }
        IGenericRepository<CategoryAttributes> CategoryAttributes { get; }
        IGenericRepository<CollectionRoutes> CollecctionRoutes { get; }
        IGenericRepository<Packages> Packages { get; }
        IGenericRepository<PointTransactions> PointTransactions { get; }
        IGenericRepository<ProductImages> ProductImages { get; }
        IGenericRepository<ProductStatusHistory> ProductStatusHistory { get; }
        IGenericRepository<Shifts> Shifts { get; }
        IGenericRepository<SmallCollectionPoints> SmallCollectionPoints { get; }
        IGenericRepository<UserPoints> UserPoints { get; }
        IGenericRepository<Vehicles> Vehicles { get; }
		IGenericRepository<CollectionGroups> CollectionGroups { get; }

		IGenericRepository<ForgotPassword> ForgotPasswords { get; }

        IGenericRepository<SystemConfig> SystemConfig { get; }
		Task<int> SaveAsync();
    }
}
