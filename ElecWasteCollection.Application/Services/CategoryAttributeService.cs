using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class CategoryAttributeService : ICategoryAttributeService
	{
		private static List<CategoryAttributes> _categoriesAttribute = FakeDataSeeder.categoryAttributes;
		private static List<Attributes> _attributes = FakeDataSeeder.attributes;
		public List<CategoryAttributeModel> GetCategoryAttributesByCategoryId(Guid categoryId)
		{
			// Tạo dictionary để truy vấn nhanh chóng thuộc tính theo AttributeId
			var attributeDictionary = _attributes.ToDictionary(a => a.AttributeId, a => a.Name);

			var categoryAttributes = _categoriesAttribute
				.Where(ca => ca.CategoryId == categoryId)
				.Select(ca => new CategoryAttributeModel
				{
					Id = ca.AttributeId,
					Name = attributeDictionary.ContainsKey(ca.AttributeId)
						   ? attributeDictionary[ca.AttributeId] // Trả về tên thuộc tính nếu tìm thấy
						   : "Không tìm thấy" // Nếu không có thuộc tính tương ứng, trả về giá trị mặc định
				})
				.ToList();

			return categoryAttributes;
		}
	}
}
