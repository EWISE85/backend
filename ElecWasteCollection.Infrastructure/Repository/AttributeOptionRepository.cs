using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.Repository
{
	public class AttributeOptionRepository : GenericRepository<AttributeOptions>, IAttributeOptionRepository
	{
		public AttributeOptionRepository(DbContext context) : base(context)
		{
		}
	}
}
