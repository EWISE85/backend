using DocumentFormat.OpenXml.Spreadsheet;
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
	public class ForgotPasswordService : IForgotPasswordService
	{
		private readonly IForgotPasswordRepository _forgotPasswordRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserRepository _userRepository;
		private readonly IEmailService _emailService;

		public ForgotPasswordService(IForgotPasswordRepository forgotPasswordRepository, IUnitOfWork unitOfWork, IUserRepository userRepository, IEmailService emailService)
		{
			_forgotPasswordRepository = forgotPasswordRepository;
			_unitOfWork = unitOfWork;
			_userRepository = userRepository;
			_emailService = emailService;
		}
		public async Task<bool> CheckOTP(string email, string otp)
		{
			var user = await _userRepository.GetAsync(u => u.Email == email);
			if (user == null) throw new AppException("Không tìm thấy tài khoản với email này", 404);
			var fp = await _forgotPasswordRepository.GetAsync(f => f.UserId == user.UserId);
			if (fp == null) throw new AppException("Không có otp dưới db", 404);
			if (fp.OTP != otp) throw new AppException("OTP không đúng", 400);
			if (fp.ExpireAt < DateTime.UtcNow) throw new AppException("OTP đã hết hạn", 400);
			_unitOfWork.ForgotPasswords.Delete(fp);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<OTPResponseModel?> GetOTPByUser(Guid userId)
		{
			var user = await _userRepository.GetAsync(u => u.UserId == userId);
			if (user == null) throw new AppException("Không tìm thấy nguời dùng này", 404);
			var fp = await _forgotPasswordRepository.GetAsync(f => f.UserId == userId);
			if (fp == null) throw new AppException("Không có otp dưới db", 404);
			var response = new OTPResponseModel
			{
				Email = user.Email,
				OTP = fp.OTP
			};
			return response;
		}

		public async Task<bool> SaveOTP(CreateForgotPasswordModel forgotPassword)
		{
			var otp = new Random().Next(100000, 999999).ToString();
			string subject = "Your OTP Code";
			string body = $"Mã OTP của bạn là: {otp}";
			await _emailService.SendEmailAsync(forgotPassword.Email, subject, body);
			var user = await _userRepository.GetAsync(u => u.Email == forgotPassword.Email);
			if (user == null) throw new AppException("Không tìm thấy tài khoản với email này", 404);
			var checkExist = await _forgotPasswordRepository.GetAsync(fp => fp.UserId == user.UserId);
			if (checkExist != null)
			{
				checkExist.OTP = otp;
				_unitOfWork.ForgotPasswords.Update(checkExist);
			}
			else
			{
				var forgotPasswordEntity = new Domain.Entities.ForgotPassword
				{
					ForgotPasswordId = Guid.NewGuid(),
					UserId = user.UserId,
					OTP = otp,
					ExpireAt = DateTime.UtcNow.AddMinutes(15),
				};
				await _unitOfWork.ForgotPasswords.AddAsync(forgotPasswordEntity);
			}
			await _unitOfWork.SaveAsync();
			return true;
		}
	}
}
