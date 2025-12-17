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
		Task<Guid> ReceivePointFromCollectionPoint(CreatePointTransactionModel createPointTransactionModel);

		Task<List<PointTransactionModel>> GetAllPointHistoryByUserId(Guid id);
	}
}
