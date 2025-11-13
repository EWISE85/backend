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

		public Guid ReceivePointFromCollectionPoint(CreatePointTransactionModel createPointTransactionModel)
		{
			var points = new PointTransactions
			{
				PointTransactionId = Guid.NewGuid(),
				PostId = createPointTransactionModel.PostId,
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
