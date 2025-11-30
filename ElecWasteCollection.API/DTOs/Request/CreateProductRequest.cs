namespace ElecWasteCollection.API.DTOs.Request
{
	public class CreateProductRequest
	{
		public Guid ParentCategoryId { get; set; }

		public Guid SubCategoryId { get; set; }

		public Guid BrandId { get; set; }


		public List<ProductValueRequest>? Attributes { get; set; }
	}
}
