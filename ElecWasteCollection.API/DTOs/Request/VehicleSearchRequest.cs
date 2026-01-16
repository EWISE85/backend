namespace ElecWasteCollection.API.DTOs.Request
{
	public class VehicleSearchRequest
	{
		public int Page { get; set; } = 1;

		public int Limit { get; set; } = 10;

		public string? PlateNumber { get; set; }

		public string? CollectionCompanyId { get; set; }

		public string? SmallCollectionPointId { get; set; }

		public string? Status { get; set; }
	}
}
