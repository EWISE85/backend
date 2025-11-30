//using ElecWasteCollection.Application.Data;
//using ElecWasteCollection.Application.IServices;
//using ElecWasteCollection.Application.Model;
//using ElecWasteCollection.Domain.Entities;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ElecWasteCollection.Application.Services
//{
//	public class SizeTierService : ISizeTierService
//	{
//		private static List<SizeTier> sizeTiers = FakeDataSeeder.sizeTiers;

//		public List<SizeTierModel> GetAllSizeTierByCategoryId(Guid categoryId)
//		{
//			var result = sizeTiers
//				.Where(st => st.CategoryId == categoryId)
//				.Select(st => new SizeTierModel
//				{
//					Id = st.SizeTierId,
//					Name = st.Name
//				})
//				.ToList();

//			return result;
//		}
//	}
//}
