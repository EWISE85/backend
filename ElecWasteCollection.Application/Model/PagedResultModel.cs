using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class PagedResultModel<T>
	{
		public int Page { get; set; }
		public int Limit { get; set; }
		public int TotalItems { get; set; }
		public int TotalPages { get; set; }
		public List<T> Data { get; set; }

		// Constructor để dễ sử dụng
		public PagedResultModel(List<T> data, int page, int limit, int totalItems)
		{
			Data = data;
			Page = page;
			Limit = limit;
			TotalItems = totalItems;
			if (limit > 0)
			{
				TotalPages = (int)Math.Ceiling(totalItems / (double)limit);
			}
			else
			{
				TotalPages = 0; 
			}
		}
	}
}
