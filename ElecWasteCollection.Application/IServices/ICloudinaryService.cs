using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface ICloudinaryService
	{
		Task<string> UploadRawFileAsync(IFormFile file, string publicId);
	}
}
