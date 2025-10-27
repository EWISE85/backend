using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class CollectorService : ICollectorService
	{
		private  Collector collectors = FakeDataSeeder.collector;


		public Collector GetById(Guid id)
		{
			var collector = collectors;
			return collector;
		}
	}
}
