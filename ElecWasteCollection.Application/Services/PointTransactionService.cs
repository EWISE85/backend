using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class PointTransactionService : IPointTransactionService
	{
		private readonly IPointTransactionRepository _pointTransactionRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserPointService _userPointService;
		private readonly IProductImageRepository _productImageRepository;

		public PointTransactionService(IPointTransactionRepository pointTransactionRepository, IUnitOfWork unitOfWork, IUserPointService userPointService, IProductImageRepository productImageRepository)
		{
			_pointTransactionRepository = pointTransactionRepository;
			_unitOfWork = unitOfWork;
			_userPointService = userPointService;
			_productImageRepository = productImageRepository;
		}

		public async Task<List<PointTransactionModel>> GetAllPointHistoryByUserId(Guid id)
		{
			var pointTransactions = await _pointTransactionRepository.GetPointHistoryWithProductImagesAsync(id);

			if (pointTransactions == null || !pointTransactions.Any())
			{
				return new List<PointTransactionModel>();
			}

			var result = pointTransactions.Select(pt =>
			{
				var images = pt.Product?.ProductImages?
					.Select(pi => pi.ImageUrl)
					.ToList() ?? new List<string>();

				return new PointTransactionModel
				{
					PointTransactionId = pt.PointTransactionId,
					ProductId = pt.ProductId,
					UserId = pt.UserId,
					Desciption = pt.Desciption,
					TransactionType = pt.TransactionType,
					Point = pt.Point,
					CreatedAt = pt.CreatedAt,

					Images = images
				};
			})
			.ToList();

			return result;
		}

		public async Task<Guid> ReceivePointFromCollectionPoint(CreatePointTransactionModel createPointTransactionModel)
		{
			var points = new PointTransactions
			{
				PointTransactionId = Guid.NewGuid(),
				ProductId = createPointTransactionModel.ProductId,
				UserId = createPointTransactionModel.UserId,
				Desciption = createPointTransactionModel.Desciption,
				Point = createPointTransactionModel.Point,
				CreatedAt = DateTime.UtcNow,
				TransactionType = PointTransactionType.Earned.ToString()
			};
			var userPoint = await _userPointService.GetPointByUserId(createPointTransactionModel.UserId);
			await _unitOfWork.PointTransactions.AddAsync(points);
			var result =  await _userPointService.UpdatePointForUser(createPointTransactionModel.UserId, points.Point);
			await _unitOfWork.SaveAsync();
			return points.PointTransactionId;
		}
	}
}
