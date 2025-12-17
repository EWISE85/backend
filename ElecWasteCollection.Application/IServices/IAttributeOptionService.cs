using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface IAttributeOptionService
    {
       Task<List<AttributeOptionResponse>> GetOptionsByAttributeId(Guid attributeId);
		Task<AttributeOptionResponse?> GetOptionByOptionId(Guid optionId);
	}
}
