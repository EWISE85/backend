using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class UpdateSystemConfigModel
	{
		public Guid SystemConfigId { get; set; }

		public IFormFile? ExcelFile { get; set; }

		public string? Value { get; set; }
	}
}
