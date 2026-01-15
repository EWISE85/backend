using ElecWasteCollection.Application.Model.AssignPost;

namespace ElecWasteCollection.Application.IServices.IAssignPost
{
    public interface IProductAssignService
    {
		//Task<AssignProductResult> AssignProductsAsync(List<Guid> productIds,DateOnly workDate); 
		Task<List<ProductByDateModel>> GetProductsByWorkDateAsync(DateOnly workDate);
		void AssignProductsInBackground(List<Guid> productIds, DateOnly workDate, string userId);
        Task<object> GetProductIdsForWorkDateAsync(DateOnly workDate);

    }
}
