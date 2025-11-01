using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IProfanityChecker
	{
		Task<bool> ContainsProfanityAsync(string text);

		// Định nghĩa hành vi: Lọc và che
		Task<string> CensorTextAsync(string text, string placeholder = "****");

		Task<bool> ContainsPhoneNumberAsync(string text);
	}
}
