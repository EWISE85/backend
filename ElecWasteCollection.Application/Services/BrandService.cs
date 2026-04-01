using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class BrandService : IBrandService
	{
		private readonly IBrandRepository _brandRepository;
		private readonly IUnitOfWork _unitOfWork;

		public BrandService(IBrandRepository brandRepository, IUnitOfWork unitOfWork)
		{
			_brandRepository = brandRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<Guid> CheckAndUpdateBrandAsync(string brandName)
		{
			var existing = await _brandRepository.GetAsync(b => b.Name.ToLower() == brandName.ToLower());
			if (existing != null) return existing.BrandId;

			var newBrand = new Brand
			{
				BrandId = Guid.NewGuid(),
				Name = brandName
			};

			await _unitOfWork.Brands.AddAsync(newBrand);
			await _unitOfWork.SaveAsync();
			return newBrand.BrandId;
		}

		public async Task<List<BrandModel>> GetBrandsByCategoryIdAsync(Guid categoryId)
		{
			var brands = await _unitOfWork.BrandCategories.GetsAsync(bc => bc.CategoryId == categoryId, includeProperties: "Brand");
			if (brands == null || !brands.Any())
			{
				return new List<BrandModel>();
			}
			var brandModels = brands.Select(b => new BrandModel
			{
				BrandId = b.BrandId,
				Name = b.Brand.Name,
				CategoryId = b.CategoryId
			}).ToList();
			return brandModels;
		}
		public async Task SyncBrandsAsync(List<string> excelBrandNames)
		{
			// 1. Lấy tất cả thương hiệu đang có trong DB
			var dbBrands = await _unitOfWork.Brands.GetAllAsync(); // Giả sử bạn có hàm lấy tất cả

			var excelNamesLower = excelBrandNames.Select(n => n.ToLower()).ToList();

			// 2. Xử lý những thương hiệu trong DB mà Excel KHÔNG có (Đổi status)
			foreach (var dbBrand in dbBrands)
			{
				if (!excelNamesLower.Contains(dbBrand.Name.ToLower()))
				{
					// Nếu bạn đã thêm trường Status vào Brand
					dbBrand.Status = BrandStatus.KHONG_HOAT_DONG.ToString();
					_unitOfWork.Brands.Update(dbBrand);
				}
				else
				{
					dbBrand.Status = BrandStatus.HOAT_DONG.ToString();
					_unitOfWork.Brands.Update(dbBrand);
				}
			}

			// 3. Xử lý những thương hiệu trong Excel mà DB CHƯA có (Thêm mới)
			foreach (var name in excelBrandNames)
			{
				if (!dbBrands.Any(b => b.Name.ToLower() == name.ToLower()))
				{
					var newBrand = new Brand
					{
						BrandId = Guid.NewGuid(),
						Name = name,
						Status = BrandStatus.HOAT_DONG.ToString() // Mặc định là hoạt động khi thêm mới
					};
					await _unitOfWork.Brands.AddAsync(newBrand);
				}
			}

			await _unitOfWork.SaveAsync();
		}

	}
}
