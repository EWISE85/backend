namespace ElecWasteCollection.API.DTOs.Request
{
	public class UpdateProductInfoRequest
	{
		public Guid CategoryId { get; set; }

		public Guid BrandId { get; set; }

		public List<string> Image { get; set; }
	}
}
