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
		private readonly IShippingNotifierService _shippingNotifierService;
		public ProductController(IProductService productService, IShippingNotifierService shippingNotifierService)
		{
			_productService = productService;
			_shippingNotifierService = shippingNotifierService;
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
		[HttpGet("from-date-to-date")]
		public IActionResult GetProductsComingToWarehouse([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, [FromQuery] string smallCollectionPointId )
		{
			var products = _productService.ProductsComeWarehouseByDate(fromDate, toDate, smallCollectionPointId);
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

		[HttpGet("user/{userId}")]
		public IActionResult GetAllProductsByUserId(Guid userId)
		{
			var products = _productService.GetAllProductsByUserId(userId);
			return Ok(products);
		}
		[HttpPost("notify-arrival/{productId}")]
		public async Task<IActionResult> NotifyCollectorArrival(Guid productId)
		{
			try
			{
				// 3. Gọi hàm bắn thông báo Real-time
				await _shippingNotifierService.NotifyUserOfCollectorArrival(productId);

				return Ok(new { message = "Đã gửi thông báo đến khách hàng thành công!" });
			}
			catch (Exception ex)
			{
				// Xử lý lỗi nếu có
				return StatusCode(500, new { message = "Lỗi khi gửi thông báo", error = ex.Message });
			}
		}
		[HttpGet("{id}")]
		public IActionResult GetProductDetailById(Guid id)
		{
			var productDetail = _productService.GetProductDetailById(id);
			if (productDetail == null)
			{
				return NotFound("Product not found.");
			}
			return Ok(productDetail);
		}

		[HttpPut("checked")]
		public IActionResult UpdateProductCheckedStatus([FromBody] CheckedProductRequest request)
		{
			var result = _productService.UpdateCheckedProductAtRecycler(request.PackageId, request.ProductQrCode);
			if (!result)
			{
				return BadRequest("Failed to update product checked status.");
			}
			return Ok(new { message = "Product checked status updated successfully." });
		}
	}
}
