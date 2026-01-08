using ElecWasteCollection.Domain.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.Repository
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(DbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, string? includeProperties = null)
        {
            IQueryable<T> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }

            return await query.ToListAsync();
        }
        public async Task<T?> GetAsync(Expression<Func<T, bool>> filter, string? includeProperties = null)
        {
            IQueryable<T> query = _dbSet;

            if (!string.IsNullOrEmpty(includeProperties))
            {
                foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }

            return await query.FirstOrDefaultAsync(filter);
        }

        public async Task<T?> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public void Update(T entity)
        {
            _dbSet.Update(entity);
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

		public async Task<List<T>> GetsAsync(Expression<Func<T, bool>> filter, string? includeProperties = null)
		{
			IQueryable<T> query = _dbSet;

			if (!string.IsNullOrEmpty(includeProperties))
			{
				foreach (var includeProp in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
				{
					query = query.Include(includeProp);
				}
			}

			return await query.Where(filter).ToListAsync();
		}
		public void Add(T entity) 
		{
			_dbSet.Add(entity);
		}
		public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null)
		{
			if (predicate == null)
			{
				return await _context.Set<T>().CountAsync();
			}
			return await _context.Set<T>().CountAsync(predicate);
		}
        public IQueryable<T> GetQueryable()
        {
            return _dbSet.AsNoTracking(); 
        }
    }
}
