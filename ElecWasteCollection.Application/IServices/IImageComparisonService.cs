using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IImageComparisonService
	{
		Task<bool> CompareImagesSimilarityAsync(List<string> urls1, List<string> urls2);
	}
}
