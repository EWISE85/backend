namespace ElecWasteCollection.API.DTOs.Request
{
	public class ProductValueRequest
	{
		public Guid AttributeId { get; set; }

		public Guid? OptionId { get; set; }

		public double? Value { get; set; }
	}
}
