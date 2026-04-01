using ElecWasteCollection.Application.Exceptions;
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
	public class AttributeOptionService : IAttributeOptionService
	{
		private readonly IAttributeOptionRepository _attributeOptionRepository;
		public AttributeOptionService(IAttributeOptionRepository attributeOptionRepository)
		{
			_attributeOptionRepository = attributeOptionRepository;
		}

		public async Task<AttributeOptionResponse?> GetOptionByOptionId(Guid optionId)
		{
			var option = await  _attributeOptionRepository.GetAsync(opt => opt.OptionId == optionId);
			if (option == null) throw new AppException("Không tìm thấy option", 404);
			var responseOption = new AttributeOptionResponse
			{
				AttributeOptionId = option.OptionId,
				OptionName = option.OptionName
			};
			return responseOption;
		}

		public async Task<List<AttributeOptionResponse>> GetOptionsByAttributeId(Guid attributeId)
		{
			var options = await _attributeOptionRepository.GetsAsync(option => option.AttributeId == attributeId&& option.Status == AttributeOptionStatus.DANG_HOAT_DONG.ToString());
			if (options == null) return new List<AttributeOptionResponse>(); 
			var responseOptions = options
				.OrderBy(option => option.EstimateWeight)
				.ThenBy(option => option.EstimateVolume)
				.Select(option => new AttributeOptionResponse
			{
				AttributeOptionId = option.OptionId,
				OptionName = option.OptionName,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<AttributeOptionStatus>(option.Status)
			}).ToList();
			return responseOptions;
		}

		public async Task<List<AttributeOptionResponse>> GetOptionsByAttributeIdForAdmin(Guid attributeId, string? status)
		{
			var options = await _attributeOptionRepository.GetsAsync(option => option.AttributeId == attributeId && (string.IsNullOrEmpty(status) || option.Status == status));
			if (options == null) return new List<AttributeOptionResponse>();
			var responseOptions = options
				.OrderBy(option => option.EstimateWeight)
				.ThenBy(option => option.EstimateVolume)
				.Select(option => new AttributeOptionResponse
			{
				AttributeOptionId = option.OptionId,
				OptionName = option.OptionName,
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<AttributeOptionStatus>(option.Status)
			}).ToList();
			return responseOptions;
		}
	}
}
