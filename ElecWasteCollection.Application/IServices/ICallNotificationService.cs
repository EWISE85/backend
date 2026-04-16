using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface ICallNotificationService
	{
		Task SendIncomingCallAsync(Guid calleeId, object callData);
		Task SendCallEndedAsync(Guid targetUserId, string callId);
	}
}
