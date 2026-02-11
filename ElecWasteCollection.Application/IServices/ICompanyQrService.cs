using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface ICompanyQrService
	{
		string GenerateQrCode(string companyId);
		Task<CollectionCompanyResponse?> VerifyQrCodeAsync(string qrCode);
	}
}
