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

		public async Task<bool> UpdatePointForUser(Guid userId, double point)
		{
			var userPoint = await _userPointRepository.GetAsync(up => up.UserId == userId);
			if (userPoint == null) throw new AppException("Không tìm thấy điểm người dùng", 404);
			userPoint.Points += point;
			_unitOfWork.UserPoints.Update(userPoint);
			await _unitOfWork.SaveAsync();
			return true;
		}
	}
}
