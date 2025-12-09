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
	public class UserPointService : IUserPointService
	{
		private readonly List<UserPoints> _userPoints = FakeDataSeeder.userPoints;
		public UserPointModel GetPointByUserId(Guid userId)
		{
			var userPoint = _userPoints
				.Where(up => up.UserId == userId)
				.Select(up => new UserPointModel
				{
					Id = up.UserPointId,
					UserId = up.UserId,
					Points = up.Points
				})
				.FirstOrDefault();
			return userPoint;
		}

		public bool UpdatePointForUser(Guid userId, double point)
		{
			var userPoint = _userPoints.FirstOrDefault(up => up.UserId == userId);
			if (userPoint != null)
			{
				userPoint.Points += point;
				return true;
			}
			return false;
		}
	}
}
