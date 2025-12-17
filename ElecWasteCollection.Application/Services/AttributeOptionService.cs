using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.Exceptions;
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
		private readonly List<AttributeOptions> _attributeOptions = FakeDataSeeder.attributeOptions;
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
			var options = await _attributeOptionRepository.GetsAsync(option => option.AttributeId == attributeId);
			if (options == null) return new List<AttributeOptionResponse>(); 
			var responseOptions = options.Select(option => new AttributeOptionResponse
			{
				AttributeOptionId = option.OptionId,
				OptionName = option.OptionName
			}).ToList();
			return responseOptions;
		}
	}
}
