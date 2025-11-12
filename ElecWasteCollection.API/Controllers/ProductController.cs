using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/products/")]
	[ApiController]
	public class ProductController : ControllerBase
	{
		private readonly IProductService _productService;
		public ProductController(IProductService productService)
		{
			_productService = productService;
		}
		[HttpGet("qrcode/{qrcode}")]
		public IActionResult GetProductByQrCode(string qrcode)
		{
			var product = _productService.GetByQrCode(qrcode);
			if (product == null)
			{
				return NotFound("Product not found.");
			}
			return Ok(product);
		}
		[HttpPut("receive-at-warehouse/{qrCode}")]
		public IActionResult ReceiveProductAtWarehouse(string qrCode)
		{
			var result = _productService.UpdateProductStatusByQrCode(qrCode,"Nhập kho");
			if (!result)
			{
				return BadRequest("Failed to update product status.");
			}
			return Ok(new { message = "Product status updated successfully." });
		}
		[HttpGet("incoming-warehouse")]
		public IActionResult GetProductsComingToWarehouse([FromQuery] int page, [FromQuery] int limit,[FromQuery] DateOnly pickUpDate, [FromQuery] int smallCollectionPointId, [FromQuery] string? status)
		{
			var products = _productService.ProductsComeWarehouseByDate(page,limit,pickUpDate, smallCollectionPointId, status);
			return Ok(products);
		}
	}
}
