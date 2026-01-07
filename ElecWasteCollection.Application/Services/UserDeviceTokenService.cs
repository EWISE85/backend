using DocumentFormat.OpenXml.Office2016.Excel;
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
	public class UserDeviceTokenService : IUserDeviceTokenService
	{
		private readonly IUnitOfWork _unitOfWork;
		public UserDeviceTokenService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		public async Task<bool> RegisterDeviceAsync(RegisterDeviceModel registerDeviceModel)
		{
			var existingDevice = await _unitOfWork.UserDeviceTokens.GetAsync(x => x.FCMToken == registerDeviceModel.FcmToken);
			if (existingDevice != null)
			{
				if (existingDevice.UserId != registerDeviceModel.UserId)
				{
					existingDevice.UserId = registerDeviceModel.UserId;
				}
				existingDevice.Platform = registerDeviceModel.Platform ?? existingDevice.Platform;

				_unitOfWork.UserDeviceTokens.Update(existingDevice);
			}
			else
			{
				var newDevice = new UserDeviceToken
				{
					UserDeviceTokenId = Guid.NewGuid(),
					UserId = registerDeviceModel.UserId,
					FCMToken = registerDeviceModel.FcmToken,
					Platform = registerDeviceModel.Platform,
					CreatedAt = DateTime.UtcNow,
				};

				await _unitOfWork.UserDeviceTokens.AddAsync(newDevice);
			}
			await _unitOfWork.SaveAsync();
			return true;
		}
	}
}
