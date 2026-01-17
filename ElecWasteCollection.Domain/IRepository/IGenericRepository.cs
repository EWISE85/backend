using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null);
        Task<T?> GetAsync(Expression<Func<T, bool>> filter, string? includeProperties = null);
		Task<List<T>> GetsAsync(Expression<Func<T, bool>> filter, string? includeProperties = null);
		Task<T?> GetByIdAsync(object id);
        Task AddAsync(T entity);
        void Update(T entity);
        void Delete(T entity);
		void Add(T entity);
        IQueryable<T> GetQueryable();
        Task<int> CountAsync(Expression<Func<T, bool>> predicate = null);
        IQueryable<T> AsQueryable();
    }
}
