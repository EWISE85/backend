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
	public class CategoryAttributeService : ICategoryAttributeService
	{
		private readonly ICategoryAttributeRepsitory _categoryAttributeRepsitory;

		public CategoryAttributeService(ICategoryAttributeRepsitory categoryAttributeRepsitory)
		{
			_categoryAttributeRepsitory = categoryAttributeRepsitory;
		}

		public async Task<List<CategoryAttributeModel>> GetCategoryAttributesByCategoryIdAsync(Guid categoryId)
		{
			var listEntities = await _categoryAttributeRepsitory.GetsAsync(x => x.CategoryId == categoryId && x.Attribute.Status == AttributeStatus.Active.ToString(),"Attribute");
			if (listEntities == null)
			{
				return new List<CategoryAttributeModel>();
			}

			// 3. Map từ Entity sang Model
			var result = listEntities.Select(ca => new CategoryAttributeModel
			{
				Id = ca.AttributeId,
				Name = ca.Attribute?.Name ?? "Không tìm thấy tên"
			}).ToList();
			return result;
		}
	}
}
