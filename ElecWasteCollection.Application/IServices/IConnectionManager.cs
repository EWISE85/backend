using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IConnectionManager
	{
		void AddConnection(Guid userId, string connectionId);
		void RemoveConnection(Guid userId);
		bool IsUserOnline(Guid userId);
		string? GetConnectionId(Guid userId);
	}
}
