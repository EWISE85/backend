using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IEmailService
	{
		Task SendEmailAsync(string to, string subject, string body);
	}
}
