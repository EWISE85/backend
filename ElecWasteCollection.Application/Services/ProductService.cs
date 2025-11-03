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
	public class ProductService : IProductService
	{
		private readonly List<Products> _products = FakeDataSeeder.products;
		public Products? GetById(Guid productId)
		{
			return _products.FirstOrDefault(p => p.Id == productId);
		}
	}
}
