using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Cld = CloudinaryDotNet;

namespace ElecWasteCollection.Infrastructure.ExternalService.Cloudinary
{
	public class CloudinaryService : ICloudinaryService
	{
		private readonly Cld.Cloudinary _cloudinary; 
		public CloudinaryService(IOptions<CloudinarySettings> config)
		{
			var acc = new Cld.Account(
				config.Value.CloudName,
				config.Value.ApiKey,
				config.Value.ApiSecret
			);
			_cloudinary = new Cld.Cloudinary(acc);
		}
		public async Task<string> UploadRawFileAsync(IFormFile file, string publicId)
		{
			using var stream = file.OpenReadStream();
			var uploadParams = new RawUploadParams
			{
				File = new FileDescription(file.FileName, stream),
				PublicId = publicId, 
				Folder = "excel_files",
				Overwrite = true,
				Invalidate = true
			};

			var result = await _cloudinary.UploadAsync(uploadParams);
			if (result.Error != null) throw new Exception(result.Error.Message);

			return result.SecureUrl.ToString();
		}
	}
}
