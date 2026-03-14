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
	}
}
