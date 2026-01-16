using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class ImportResult
	{
		public bool Success { get; set; }
		public List<string> Messages { get; set; } = new();
	}
}
