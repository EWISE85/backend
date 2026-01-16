using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IWebNotificationService
	{
		Task SendNotificationAsync(string userId, string title, string message, string type = "info", object? data = null);
	}
}
