namespace ElecWasteCollection.API.DTOs.Request
{
	public class ImageComparisonRequest
	{
		public List<string> ProductImages { get; set; }

		public List<string> ConfirmImages { get; set; }
	}
}
