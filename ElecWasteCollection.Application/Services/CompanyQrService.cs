using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class CompanyQrService : ICompanyQrService
	{
		private readonly IMemoryCache _cache;
		private readonly ICompanyService _companyService;
		private readonly ICompanyRepository _companyRepository;
		private readonly IPackageRepository _packageRepository;
		public CompanyQrService(IMemoryCache cache, ICompanyService companyService, ICompanyRepository companyRepository, IPackageRepository packageRepository)
		{
			_cache = cache;
			_companyService = companyService;
			_companyRepository = companyRepository;
			_packageRepository = packageRepository;
		}
		public string GenerateQrCode(string companyId)
		{
			int shortId = QrMathHelper.GetStableShortId(companyId);
			return QrMathHelper.Encrypt(shortId);
		}

		public async Task<CollectionCompanyResponse?> VerifyQrCodeAsync(string qrCode)
		{
			var result = QrMathHelper.Decrypt(qrCode);
			if (!result.IsTimeValid) throw new AppException("Qr code giao hàng đã hết hạn sử dụng", 400);
			var isQrCodeUsed = await _packageRepository.GetAsync(p => p.DeliveryQrCode == qrCode);
			if (isQrCodeUsed != null) throw new AppException("Qr code giao hàng đã được sử dụng",400);

			var mapping = await GetCompanyMappingAsync();
			if (mapping.TryGetValue(result.ShortId, out string? realCompanyId))
			{
				var company = await _companyService.GetCompanyById(realCompanyId);
				return company;
			}

			return null;
		}
		private async Task<Dictionary<int, string>> GetCompanyMappingAsync()
		{
			var result =  await _cache.GetOrCreateAsync("Map_Hash_CompanyId", async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

				var allCompanyIds = await _companyRepository.GetAllCompanyIdsAsync();

				var dict = new Dictionary<int, string>();
				foreach (var id in allCompanyIds)
				{
					int hash = QrMathHelper.GetStableShortId(id);

					
					if (!dict.ContainsKey(hash))
					{
						dict.Add(hash, id);
					}
				}
				return dict;
			});
			return result ?? new Dictionary<int, string>();
		}
	}
}
