namespace ElecWasteCollection.API.DTOs.Request
{
    public class RouteSearchQueryRequest
    {
		public int Page { get; set; }

		public int Limit { get; set; }

		public string? CollectionPointId { get; set; }
		public DateOnly? PickUpDate { get; set; }

		public string? Status { get; set; }
	}
}
