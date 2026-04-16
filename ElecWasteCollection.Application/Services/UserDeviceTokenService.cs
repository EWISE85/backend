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
		public async Task<bool> RegisterDeviceAsync(RegisterDeviceModel model)
		{
			var existingDevice = await _unitOfWork.UserDeviceTokens
				.GetAsync(x => x.UserId == model.UserId && x.Platform == model.Platform);

			if (existingDevice != null)
			{
				if (!string.IsNullOrEmpty(model.FcmToken))
				{
					existingDevice.FCMToken = model.FcmToken;
				}
				if (!string.IsNullOrEmpty(model.VoipToken))
				{
					existingDevice.VoipToken = model.VoipToken;
				}

				_unitOfWork.UserDeviceTokens.Update(existingDevice);
			}
			else
			{
				var newDevice = new UserDeviceToken
				{
					UserDeviceTokenId = Guid.NewGuid(),
					UserId = model.UserId,
					FCMToken = model.FcmToken,
					VoipToken = model.VoipToken, 
					Platform = model.Platform ?? DevicePlatform.Android.ToString(),
					CreatedAt = DateTime.UtcNow
				};

				await _unitOfWork.UserDeviceTokens.AddAsync(newDevice);
			}

			await _unitOfWork.SaveAsync();
			return true;
		}
	}
}
