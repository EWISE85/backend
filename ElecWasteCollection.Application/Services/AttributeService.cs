using ElecWasteCollection.Application.Helper;
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
	public class AttributeService : IAttributeService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAttributeRepository _attributeRepository;
		private readonly IAttributeOptionRepository _attributeOptionRepository;

		public AttributeService(IUnitOfWork unitOfWork, IAttributeRepository attributeRepository, IAttributeOptionRepository attributeOptionRepository)
		{
			_unitOfWork = unitOfWork;
			_attributeRepository = attributeRepository;
			_attributeOptionRepository = attributeOptionRepository;
		}

		public async Task<Guid> EnsureAttributeExistsAsync(Attributes attribute)
		{
			var existing = await _attributeRepository.GetAsync(a => a.Name.ToLower() == attribute.Name.ToLower());
			if (existing != null) return existing.AttributeId;
			attribute.AttributeId = Guid.NewGuid();
			attribute.Status = AttributeStatus.DANG_HOAT_DONG.ToString();
			await _unitOfWork.Attributes.AddAsync(attribute);
			await _unitOfWork.SaveAsync();
			return attribute.AttributeId;
		}

		public async Task<List<AttributeModel>> GetAttributeForAdmin(string? status)
		{
			string statusEnum = null;
			if(status != null)
			{
				statusEnum = StatusEnumHelper.GetValueFromDescription<AttributeStatus>(status).ToString();
			}
			var entities = await _attributeRepository.GetsAsync(a => a.Status == statusEnum);
			if (entities == null) return new List<AttributeModel>();
			var result = entities.Select(a => new AttributeModel
			{
				Id = a.AttributeId,
				Name = a.Name,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<AttributeStatus>(a.Status)
			}).ToList();
			return result;

		}

		public async Task SyncAttributeOptionsAsync(string attrName, List<AttributeOptions> excelOptions)
		{
			// 1. Lấy ID của Attribute
			var attrId = await EnsureAttributeExistsAsync(new Attributes { Name = attrName });

			// 2. Lấy danh sách hiện tại trong DB
			var dbOptions = await _attributeOptionRepository.GetsAsync(o => o.AttributeId == attrId);

			// 3. Tắt các Option "mồ côi" (DB có nhưng Excel không còn)
			foreach (var dbOpt in dbOptions)
			{
				bool stillExists = excelOptions.Any(e => e.OptionName.Trim().ToLower() == dbOpt.OptionName.Trim().ToLower());
				if (!stillExists)
				{
					dbOpt.Status = AttributeOptionStatus.KHONG_HOAT_DONG.ToString();
					_unitOfWork.AttributeOptions.Update(dbOpt);
				}
			}

			// 4. Cập nhật hoặc Thêm mới từ Excel
			foreach (var excelOpt in excelOptions)
			{
				var match = dbOptions.FirstOrDefault(d => d.OptionName.Trim().ToLower() == excelOpt.OptionName.Trim().ToLower());
				if (match != null)
				{
					match.EstimateWeight = excelOpt.EstimateWeight;
					match.EstimateVolume = excelOpt.EstimateVolume;
					match.Status = AttributeOptionStatus.DANG_HOAT_DONG.ToString();
				}
				else
				{
					excelOpt.OptionId = Guid.NewGuid();
					excelOpt.AttributeId = attrId;
					excelOpt.Status = AttributeOptionStatus.DANG_HOAT_DONG.ToString();
					await _unitOfWork.AttributeOptions.AddAsync(excelOpt);
				}
			}

			await _unitOfWork.SaveAsync();
		}

		public async Task UpsertOptionAsync(AttributeOptions option)
		{
			var existing = await _attributeOptionRepository.GetAsync(o => o.AttributeId == option.AttributeId
								 && o.OptionName.ToLower() == option.OptionName.ToLower());
			if (existing != null)
			{
				existing.EstimateWeight = option.EstimateWeight;
				existing.EstimateVolume = option.EstimateVolume;
				existing.Status = AttributeOptionStatus.DANG_HOAT_DONG.ToString();
				 _unitOfWork.AttributeOptions.Update(existing);
			}
			else
			{
				option.OptionId = Guid.NewGuid();
				option.Status = AttributeOptionStatus.DANG_HOAT_DONG.ToString();
				await _unitOfWork.AttributeOptions.AddAsync(option);
			}
			await _unitOfWork.SaveAsync();
		}
	}
}
