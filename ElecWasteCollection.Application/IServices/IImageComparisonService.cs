using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IImageComparisonService
	{
		Task<double> CompareImageSimilarityAsync(string url1, string url2);
	}
}
