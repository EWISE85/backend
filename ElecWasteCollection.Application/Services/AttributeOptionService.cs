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
	public class AttributeOptionService : IAttributeOptionService
	{
		private readonly List<AttributeOptions> _attributeOptions = FakeDataSeeder.attributeOptions;

		public AttributeOptionResponse? GetOptionByOptionId(Guid optionId)
		{
			var option = _attributeOptions
				.Where(opt => opt.OptionId == optionId)
				.Select(opt => new AttributeOptionResponse
				{
					AttributeOptionId = opt.OptionId,
					OptionName = opt.OptionName
				})
				.FirstOrDefault();

			return option;
		}

		public List<AttributeOptionResponse> GetOptionsByAttributeId(Guid attributeId)
		{
			var options = _attributeOptions
				.Where(option => option.AttributeId == attributeId)
				.Select(option => new AttributeOptionResponse
				{
					AttributeOptionId = option.OptionId,
					OptionName = option.OptionName
				})
				.ToList();

			return options;
		}
	}
}
