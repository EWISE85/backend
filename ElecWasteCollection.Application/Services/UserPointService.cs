using ElecWasteCollection.Application.Data;
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
	public class UserPointService : IUserPointService
	{
		private readonly IUserPointRepository _userPointRepository;
		private readonly IUnitOfWork _unitOfWork;
		public UserPointService(IUserPointRepository userPointRepository, IUnitOfWork unitOfWork)
		{
			_userPointRepository = userPointRepository;
			_unitOfWork = unitOfWork;
		}
		public async Task<UserPointModel> GetPointByUserId(Guid userId)
		{
			var userPoint = await _userPointRepository.GetAsync(up => up.UserId == userId);
			if (userPoint == null) throw new AppException("Không tìm thấy điểm người dùng", 404);
			var userPointModel = new UserPointModel
			{
				UserId = userPoint.UserId,
				Points = userPoint.Points
			};
			return userPointModel;
		}

		public async Task<bool> UpdatePointForUser(Guid userId, double pointToAdd)
		{
			// 1. Tìm xem user đã có record trong bảng UserPoints chưa
			var userPoint = await _unitOfWork.UserPoints.GetAsync(up => up.UserId == userId);

			if (userPoint == null)
			{
				// KHÔNG THROW EXCEPTION Ở ĐÂY
				// Nếu chưa tìm thấy -> Tự động Tạo Mới (Insert)
				var newUserPoint = new UserPoints
				{
					UserPointId = Guid.NewGuid(),
					UserId = userId,
					Points = pointToAdd, 
				};
				await _unitOfWork.UserPoints.AddAsync(newUserPoint);
			}
			else
			{
				// Nếu đã có -> Cộng dồn (Update)
				userPoint.Points += pointToAdd;
				_unitOfWork.UserPoints.Update(userPoint);
			}
			return true;
		}
	}
}
