using ElecWasteCollection.Application.Exceptions;
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
		private readonly IProductImageRepository _productImageRepository;
		private readonly IUserService _userService;
		private readonly INotificationService _notificationService;
		public PointTransactionService(IPointTransactionRepository pointTransactionRepository, IUnitOfWork unitOfWork, IProductImageRepository productImageRepository, IUserService userService, INotificationService notificationService)
		{
			_pointTransactionRepository = pointTransactionRepository;
			_unitOfWork = unitOfWork;
			_productImageRepository = productImageRepository;
			_userService = userService;
			_notificationService = notificationService;
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
				var images = pt.Product?.Images?
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

		public async Task<Guid> ReceivePointFromCollectionPoint(CreatePointTransactionModel model, bool saveChanges = true)
		{
			var points = new PointTransactions
			{
				PointTransactionId = Guid.NewGuid(),
				ProductId = model.ProductId,
				UserId = model.UserId,
				Desciption = model.Desciption,
				Point = model.Point,
				CreatedAt = DateTime.UtcNow,
				TransactionType = PointTransactionType.TICH_DIEM.ToString()
			};

			await _unitOfWork.PointTransactions.AddAsync(points);

			await _userService.UpdatePointForUser(model.UserId, points.Point);

			if (saveChanges)
			{
				await _unitOfWork.SaveAsync();
			}

			return points.PointTransactionId;
		}

		public async Task<bool> UpdatePointByProductId(Guid productId, double newPointValue, string reason = "Cập nhật lại loại sản phẩm/Brand")
		{
			var originalTrans = await _unitOfWork.PointTransactions.GetAsync(t => t.ProductId == productId && t.TransactionType == PointTransactionType.TICH_DIEM.ToString());

			if (originalTrans == null) throw new AppException("Không tìm thấy giao dịch",404);

			double delta = newPointValue - originalTrans.Point;
			if (delta == 0) return true;

			var adjustmentTrans = new PointTransactions
			{
				PointTransactionId = Guid.NewGuid(),
				ProductId = productId,
				UserId = originalTrans.UserId,
				Desciption = $"Do {reason} của sản phẩm. (Số điểm cũ: {originalTrans.Point} -> Mới: {newPointValue})",
				Point = delta,
				CreatedAt = DateTime.UtcNow,
				TransactionType = PointTransactionType.DIEU_CHINH.ToString()
			};
			await _unitOfWork.PointTransactions.AddAsync(adjustmentTrans);

			await _userService.UpdatePointForUser(originalTrans.UserId, delta);

			await _notificationService.NotifyPointAdjustmentAsync(originalTrans.UserId, delta, newPointValue, reason);

			await _unitOfWork.SaveAsync() ;
			return true;
		}
		public async Task<bool> RevertPointFromCollectionPoint(Guid productId, Guid userId, bool saveChanges = true)
		{
			var productTransactions = await _unitOfWork.PointTransactions.GetsAsync(pt => pt.ProductId == productId);
			
			if (productTransactions == null || !productTransactions.Any())
			{
				return true;
			}
			double netPointsToRevert = productTransactions.Sum(pt => pt.Point);
			if (netPointsToRevert <= 0)
			{
				return true;
			}
			await _userService.UpdatePointForUser(userId, -netPointsToRevert);
			_unitOfWork.PointTransactions.DeleteRange(productTransactions);
			if (saveChanges)
			{
				await _unitOfWork.SaveAsync();
			}

			return true;
		}

		public async Task<bool> ReceivePointDaily(Guid userId, double point)
		{
			var user = await _unitOfWork.Users.GetAsync(u => u.UserId == userId);
			if (user == null)
			{
				throw new AppException("Người dùng không tồn tại", 404);
			}
			if (point < 0)
			{
				throw new AppException("Số điểm hàng ngày phải là số dương", 400);
			}
			var pointTransaction = new PointTransactions
			{
				UserId = userId,
				Desciption = "Nhận điểm hàng ngày",
				TransactionType = PointTransactionType.TICH_DIEM.ToString(),
				Point = point,
				CreatedAt = DateTime.UtcNow
			};
			user.Points += point;
			_unitOfWork.PointTransactions.Add(pointTransaction);
			_unitOfWork.Users.Update(user);
			var result = await _unitOfWork.SaveAsync();
			return result > 0 ;
		}
	}
}
