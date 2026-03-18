namespace ElecWasteCollection.API.DTOs.Request
{
	public class ImportFormatExcelRequest
	{
		public  Guid SystemConfigId { get; set; }

		public IFormFile FormFile { get; set; }
	}
}
