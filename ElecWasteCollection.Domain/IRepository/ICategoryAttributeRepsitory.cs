using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface ICategoryAttributeRepsitory : IGenericRepository<CategoryAttributes>	
	{
		Task<List<CategoryAttributes>> GetCategoryAttributeForAdmin(Guid categoryId, string? status);
	}
}
