using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IAttributeService
	{

		Task<Guid> EnsureAttributeExistsAsync(Attributes attribute);
		Task UpsertOptionAsync(AttributeOptions option);
		Task SyncAttributeOptionsAsync(string attrName, List<AttributeOptions> excelOptions);
		Task<List<AttributeModel>> GetAttributeForAdmin(string? status);

	}
}
