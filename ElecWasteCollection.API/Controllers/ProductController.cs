using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
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
		public IActionResult ReceiveProductAtWarehouse([FromRoute] string qrCode, [FromBody] UserReceivePointFromCollectionPointRequest request)
		{
			var model = new UserReceivePointFromCollectionPointModel
			{
				ProductId = request.ProductId,
				Description = request.Description,
				Point = request.Point
			};
			var result = _productService.UpdateProductStatusByQrCodeAndPlusUserPoint(qrCode,"Nhập kho", model);

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
		[HttpPost("warehouse")]
		public IActionResult AddProductToWarehouse([FromBody] CreateProductAtWarehouseRequest newProduct)
		{
			if (newProduct == null)
			{
				return BadRequest("Invalid data.");
			}

			var model = new CreateProductAtWarehouseModel
			{
				QrCode = newProduct.QrCode,
				ParentCategoryId = newProduct.ParentCategoryId,
				SubCategoryId = newProduct.SubCategoryId,
				SmallCollectionPointId = newProduct.SmallCollectionPointId,
				BrandId = newProduct.BrandId,
				Images = newProduct.Images,
				Description = newProduct.Description,
				Point = newProduct.Point,
				SenderId = newProduct.SenderId
			};
			var result = _productService.AddProduct(model);
			if (result == null)
			{
				return StatusCode(400, "An error occurred while adding the product to warehouse.");
			}

			return Ok(new { message = "Product added to warehouse successfully.", item = result });
		}
	}
}
