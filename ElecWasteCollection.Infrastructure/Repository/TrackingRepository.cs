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
	public class TrackingRepository : GenericRepository<ProductStatusHistory>, ITrackingRepository
	{
		public TrackingRepository(DbContext context) : base(context)
		{
		}
	}
}
