using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
    public interface IProductQueryRepository
    {
        Task<(List<Products> Items, int TotalCount)>
        GetPagedSmallPointProductsAsync(
            string smallPointId,
            DateOnly workDate,
            int page,
            int limit);
    }
}
