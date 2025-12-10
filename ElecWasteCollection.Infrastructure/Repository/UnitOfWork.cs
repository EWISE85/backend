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

        public UnitOfWork(ElecWasteCollectionDbContext context)
        {
            _context = context;

            Users = new GenericRepository<User>(_context);
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
