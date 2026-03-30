using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class BrandCategoryService : IBrandCategoryService
	{
		private readonly IBrandCategoryRepository _brandCategoryRepository;

		public BrandCategoryService(IBrandCategoryRepository brandCategoryRepository)
		{
			_brandCategoryRepository = brandCategoryRepository;
		}

		public async Task<double> EstimatePointForBrandAndCategory(Guid categoryId, Guid brandId)
		{
			var brandCategory = await _brandCategoryRepository.GetAsync(bc => bc.CategoryId == categoryId && bc.BrandId == brandId);
			if (brandCategory == null) throw new AppException("Không tìm thấy loại hoặc hãng",404);
			return brandCategory.Points;
		}
	}
}
