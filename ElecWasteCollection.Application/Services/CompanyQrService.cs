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
		private readonly IUnitOfWork _unitOfWork;
		public CompanyQrService(IMemoryCache cache, ICompanyService companyService, ICompanyRepository companyRepository, IPackageRepository packageRepository, IUnitOfWork unitOfWork)
		{
			_cache = cache;
			_companyService = companyService;
			_companyRepository = companyRepository;
			_packageRepository = packageRepository;
			_unitOfWork = unitOfWork;
		}
		public string GenerateQrCode(string companyId)
		{
			int shortId = QrMathHelper.GetStableShortId(companyId);
			return QrMathHelper.Encrypt(shortId);
		}

		public async Task<CollectionCompanyResponse?> VerifyQrCodeAsync(string qrCode, string collectionUnitId)
		{
			var result = QrMathHelper.Decrypt(qrCode);
			if (!result.IsTimeValid) throw new AppException("Qr code giao hàng đã hết hạn sử dụng", 400);

			var isQrCodeUsed = await _packageRepository.GetAsync(p => p.DeliveryQrCode == qrCode);
			if (isQrCodeUsed != null) throw new AppException("Qr code giao hàng đã được sử dụng", 400);

			var mapping = await GetCompanyMappingAsync();
			if (!mapping.TryGetValue(result.ShortId, out string? realCompanyId))
			{
				throw new AppException("Mã QR không hợp lệ hoặc không tìm thấy thông tin công ty", 400);
			}

			var collectionUnit = await _unitOfWork.CollectionUnits.GetAsync(c => c.CollectionUnitId == collectionUnitId);

			if (collectionUnit == null)
			{
				throw new AppException("Trạm thu gom không tồn tại", 404);
			}

			if (collectionUnit.CompanyId != realCompanyId)
			{
				throw new AppException("Công ty của bạn không có quyền xác nhận giao/nhận hàng tại đơn vị thu gom này", 400);
			}

			var company = await _companyService.GetCompanyById(realCompanyId);
			return company;
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
