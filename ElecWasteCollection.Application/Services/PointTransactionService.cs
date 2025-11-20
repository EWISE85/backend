using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class PointTransactionService : IPointTransactionService
	{
		private readonly List<PointTransactions> pointTransactions = FakeDataSeeder.points;
		private readonly IUserPointService _userPointService;

		public PointTransactionService(IUserPointService userPointService)
		{
			_userPointService = userPointService;
		}

		public List<PointTransactionModel> GetAllPointHistoryByUserId(Guid id)
		{
			var result = pointTransactions.Where(x => x.UserId == id).Select(pt => new PointTransactionModel
			{
				PointTransactionId = pt.PointTransactionId,
				PostId = pt.PostId,
				ProductId = pt.ProductId,
				UserId = pt.UserId,
				Desciption = pt.Desciption,
				TransactionType = pt.TransactionType,
				Point = pt.Point,
				CreatedAt = pt.CreatedAt
			}).ToList();

			return result;
		}

		public Guid ReceivePointFromCollectionPoint(CreatePointTransactionModel createPointTransactionModel)
		{
			var points = new PointTransactions
			{
				PointTransactionId = Guid.NewGuid(),
				PostId = createPointTransactionModel.PostId,
				ProductId = createPointTransactionModel.ProductId,
				UserId = createPointTransactionModel.UserId,
				Desciption = createPointTransactionModel.Desciption,
				Point = createPointTransactionModel.Point,
				CreatedAt = DateTime.UtcNow,
				TransactionType = PointTransactionType.Earned.ToString()
			};
			var userPoint = _userPointService.GetPointByUserId(createPointTransactionModel.UserId);
			pointTransactions.Add(points);
			var result =  _userPointService.UpdatePointForUser(createPointTransactionModel.UserId, points.Point);
			return points.PointTransactionId;
		}
	}
}
