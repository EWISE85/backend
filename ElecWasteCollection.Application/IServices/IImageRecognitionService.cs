using ElecWasteCollection.Application.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IImageRecognitionService
	{
		Task<ImaggaCheckResult> AnalyzeImageCategoryAsync(string imageUrl, string category);
	}
}
