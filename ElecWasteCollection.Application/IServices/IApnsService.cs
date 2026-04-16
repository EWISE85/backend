using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IApnsService
	{
		/// <summary>
		/// Gửi thông báo VoIP Push tới thiết bị iOS qua Apple APNs
		/// </summary>
		/// <param name="deviceToken">VoIP Token lấy từ PushKit trên iPhone</param>
		/// <param name="payload">Dữ liệu cuộc gọi (Call ID, Caller Name, Room ID...)</param>
		/// <returns>True nếu Apple xác nhận đã nhận lệnh</returns>
		Task<bool> SendVoipPushAsync(string deviceToken, object payload);
	}
}
