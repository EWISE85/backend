using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class ImaggaTag
	{
		public double Confidence { get; set; }
		public Dictionary<string, string> Tag { get; set; }
	}

	public class ImaggaResult
	{
		public List<ImaggaTag> Tags { get; set; }
	}

	public class ImaggaResponse
	{
		public ImaggaResult Result { get; set; }
	}
}
