namespace ElecWasteCollection.API.DTOs.Request
{
    public class UpdatePackageRequest
    {
		public string SmallCollectionPointsId { get; set; }

		public List<string> ProductsQrCode { get; set; }
	}
}
