using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IPointTransactionService
	{
		Task<Guid> ReceivePointFromCollectionPoint(CreatePointTransactionModel createPointTransactionModel, bool saveChanges = true);

		Task<List<PointTransactionModel>> GetAllPointHistoryByUserId(Guid id);

		Task<bool> UpdatePointByProductId(Guid productId, double newPointValue, string reason = "Cập nhật lại loại sản phẩm/Brand");
	}
}
