using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class CategoryAttributeService : ICategoryAttributeService
	{
		private readonly ICategoryAttributeRepsitory _categoryAttributeRepsitory;
		private readonly ICategoryRepository _categoryRepository;
		private readonly IAttributeRepository _attributeRepository;
		private readonly IUnitOfWork _unitOfWork;

		public CategoryAttributeService(ICategoryAttributeRepsitory categoryAttributeRepsitory, ICategoryRepository categoryRepository, IAttributeRepository attributeRepository, IUnitOfWork unitOfWork)
		{
			_categoryAttributeRepsitory = categoryAttributeRepsitory;
			_categoryRepository = categoryRepository;
			_attributeRepository = attributeRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<List<CategoryAttributeModel>> GetAttributeByCategoryIdForAdmin(Guid categoryId, string? status)
		{
			var listEntities = await _categoryAttributeRepsitory.GetsAsync(x => x.CategoryId == categoryId && (string.IsNullOrEmpty(status) || x.Status == status), "Attribute");
			if (listEntities == null)
			{
				return new List<CategoryAttributeModel>();
			}
			var result = listEntities.Select(ca => new CategoryAttributeModel
			{
				Id = ca.CategoryAttributeId,
				Name = ca.Attribute?.Name ?? "Không tìm thấy tên",
				MinValue = ca.MinValue,
				MaxValue = ca.MaxValue,
				Unit = ca.Unit,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<CategoryAttributeStatus>(ca.Status)
			}).ToList();
			return result;
		}

		

		public async Task<List<CategoryAttributeModel>> GetCategoryAttributesByCategoryIdAsync(Guid categoryId)
		{
			var listEntities = await _categoryAttributeRepsitory.GetsAsync(x => x.CategoryId == categoryId && x.Attribute.Status == AttributeStatus.DANG_HOAT_DONG.ToString() && x.Status == CategoryAttributeStatus.HOAT_DONG.ToString(),"Attribute");
			if (listEntities == null)
			{
				return new List<CategoryAttributeModel>();
			}

			// 3. Map từ Entity sang Model
			var result = listEntities.Select(ca => new CategoryAttributeModel
			{
				Id = ca.AttributeId,
				Name = ca.Attribute?.Name ?? "Không tìm thấy tên",
				MinValue = ca.MinValue
			}).ToList();
			return result;
		}

		public async Task SyncCategoryAttributeMapsAsync(List<CategoryAttributeMapModel> excelMaps)
		{
			var dbMaps = await _unitOfWork.CategoryAttributes.GetAllAsync();
			var categories = await _unitOfWork.Categories.GetAllAsync();
			var attributes = await _unitOfWork.Attributes.GetAllAsync();

			foreach (var dbMap in dbMaps)
			{
				var cat = categories.FirstOrDefault(c => c.CategoryId == dbMap.CategoryId);
				var attr = attributes.FirstOrDefault(a => a.AttributeId == dbMap.AttributeId);

				if (cat != null && attr != null)
				{
					bool stillExists = excelMaps.Any(x =>
						x.CategoryName.Trim().Equals(cat.Name.Trim(), StringComparison.OrdinalIgnoreCase) &&
						x.AttributeName.Trim().Equals(attr.Name.Trim(), StringComparison.OrdinalIgnoreCase));

					if (!stillExists && dbMap.Status != CategoryAttributeStatus.KHONG_HOAT_DONG.ToString())
					{
						dbMap.Status = CategoryAttributeStatus.KHONG_HOAT_DONG.ToString();
						_unitOfWork.CategoryAttributes.Update(dbMap);
					}
				}
			}

			// 2. UPSERT: Thêm mới hoặc Cập nhật (Kèm chốt chặn Status)
			foreach (var map in excelMaps)
			{
				var category = categories.FirstOrDefault(c => c.Name.Trim().Equals(map.CategoryName.Trim(), StringComparison.OrdinalIgnoreCase));
				var attribute = attributes.FirstOrDefault(a => a.Name.Trim().Equals(map.AttributeName.Trim(), StringComparison.OrdinalIgnoreCase));

				if (category != null && attribute != null)
				{
					var existingMap = dbMaps.FirstOrDefault(ca => ca.CategoryId == category.CategoryId && ca.AttributeId == attribute.AttributeId);

					// CHỐT CHẶN: Cả Cha (Category) và Cha (Attribute) phải sống thì Map mới được sống
					var targetStatus = (category.Status == CategoryStatus.HOAT_DONG.ToString() && attribute.Status == AttributeStatus.DANG_HOAT_DONG.ToString())
										? CategoryAttributeStatus.HOAT_DONG.ToString()
										: CategoryAttributeStatus.KHONG_HOAT_DONG.ToString();

					if (existingMap != null)
					{
						bool isChanged = existingMap.Unit != map.Unit ||
										 existingMap.MinValue != map.MinValue ||
										 existingMap.MaxValue != map.MaxValue ||
										 existingMap.Status != targetStatus;

						if (isChanged)
						{
							existingMap.Unit = map.Unit;
							existingMap.MinValue = map.MinValue;
							existingMap.MaxValue = map.MaxValue;
							existingMap.Status = targetStatus;
							_unitOfWork.CategoryAttributes.Update(existingMap);
						}
					}
					else
					{
						var newMap = new CategoryAttributes
						{
							CategoryAttributeId = Guid.NewGuid(),
							CategoryId = category.CategoryId,
							AttributeId = attribute.AttributeId,
							Unit = map.Unit,
							MinValue = map.MinValue,
							MaxValue = map.MaxValue,
							Status = targetStatus // Gán theo trạng thái của cha
						};
						await _unitOfWork.CategoryAttributes.AddAsync(newMap);
					}
				}
			}

			await _unitOfWork.SaveAsync();
		}
	}
}
