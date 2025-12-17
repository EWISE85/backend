using ElecWasteCollection.Application.Data;
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
		private readonly List<Brand> _brands = FakeDataSeeder.brands;
		private readonly IBrandRepository _brandRepository;
		public BrandService(IBrandRepository brandRepository)
		{
			_brandRepository = brandRepository;
		}
		public async Task<List<BrandModel>> GetBrandsByCategoryIdAsync(Guid categoryId)
		{
			var brands = await _brandRepository
				.GetsAsync(b => b.CategoryId == categoryId);
			if (brands == null || !brands.Any())
			{
				return new List<BrandModel>();
			}
			var brandModels = brands.Select(b => new BrandModel
			{
				BrandId = b.BrandId,
				Name = b.Name,
				CategoryId = b.CategoryId
			}).ToList();
			return brandModels;
		}
	}
}
