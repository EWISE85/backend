namespace ElecWasteCollection.API.DTOs.Request
{
	public class UpdateSystemConfigRequest
	{
		public IFormFile? ExcelFile { get; set; }
		public string? Value { get; set; }
	}
}
