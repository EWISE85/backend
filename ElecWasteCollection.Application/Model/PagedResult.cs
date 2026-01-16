using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class PagedResult<T> where T : class
	{
		/// <summary>
		/// Trang hiện tại
		/// </summary>
		public int Page { get; set; }

		/// <summary>
		/// Số lượng item mỗi trang
		/// </summary>
		public int Limit { get; set; }

		/// <summary>
		/// Tổng số item (tất cả các trang)
		/// </summary>
		public int TotalItems { get; set; }

		/// <summary>
		/// Tổng số trang
		/// </summary>
		public int TotalPages => (int)Math.Ceiling((double)TotalItems / Limit);
		/// <summary>
		/// Danh sách dữ liệu của trang hiện tại
		/// </summary>
		public List<T> Data { get; set; }

		
	}
}
