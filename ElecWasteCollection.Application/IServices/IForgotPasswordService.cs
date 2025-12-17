using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IForgotPasswordService
	{
		Task<bool> SaveOTP(CreateForgotPasswordModel forgotPassword);

		Task<OTPResponseModel?> GetOTPByUser (Guid userId);

		Task<bool> CheckOTP(string email, string otp);
	}
}
