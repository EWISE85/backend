using ElecWasteCollection.Application.Model.AssignPost;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices.IAssignPost
{
    public interface IProductAssignService
    {
		//Task<AssignProductResult> AssignProductsAsync(List<Guid> productIds,DateOnly workDate); 
		Task<List<ProductByDateModel>> GetProductsByWorkDateAsync(DateOnly workDate);
		void AssignProductsInBackground(List<Guid> productIds, DateOnly workDate, string userId);

	}
}
