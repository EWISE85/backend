using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.AssignPost;

namespace ElecWasteCollection.Application.IServices.IAssignPost
{
    public interface IProductAssignService
    {
		Task<List<ProductByDateModel>> GetProductsByWorkDateAsync(DateOnly workDate);
        Task<object> GetProductIdsForWorkDateAsync(DateOnly workDate);
        void AssignProductsInBackground(List<Guid> productIds, DateOnly workDate, string userId, List<string>? targetCompanyIds = null);
        Task<RejectProductResponse> RejectProductsAsync(RejectProductRequest request);

    }
}
