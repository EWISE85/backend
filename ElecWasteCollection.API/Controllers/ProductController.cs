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
		private const string NHAP_KHO = "Nhập kho";
		public ProductController(IProductService productService, IShippingNotifierService shippingNotifierService)
		{
			_productService = productService;
			_shippingNotifierService = shippingNotifierService;
		}
		[HttpGet("qrcode/{qrcode}")]
		public async Task<IActionResult> GetProductByQrCode(string qrcode)
		{
			var product = await _productService.GetByQrCode(qrcode);
			if (product == null)
			{
				return NotFound("Product not found.");
			}
			return Ok(product);
		}
		[HttpPut("receive-at-warehouse/{qrCode}")]
		public async Task<IActionResult> ReceiveProductAtWarehouse([FromRoute] string qrCode, [FromBody] UserReceivePointFromCollectionPointRequest request)
		{
			var model = new UserReceivePointFromCollectionPointModel
			{
				ProductId = request.ProductId,
				Description = request.Description,
				Point = request.Point
			};

			var result = await _productService.UpdateProductStatusByQrCodeAndPlusUserPoint(qrCode, NHAP_KHO, model);

			if (!result)
			{
				return BadRequest("Failed to update product status.");
			}
			return Ok(new { message = "Product status updated successfully." });
		}
		[HttpGet("from-date-to-date")]
		public async Task<IActionResult> GetProductsComingToWarehouse([FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate, [FromQuery] string smallCollectionPointId)
		{
			var products = await _productService.ProductsComeWarehouseByDateAsync(fromDate, toDate, smallCollectionPointId);
			return Ok(products);
		}
		[HttpPost("warehouse")]
		public async Task<IActionResult> AddProductToWarehouse([FromBody] CreateProductAtWarehouseRequest newProduct)
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
			var result = await _productService.AddProduct(model);
			if (result == null)
			{
				return StatusCode(400, "An error occurred while adding the product to warehouse.");
			}

			return Ok(new { message = "Product added to warehouse successfully.", item = result });
		}

		[HttpGet("user/filter")]
		public async Task<IActionResult> GetAllProductsByUserId([FromQuery] FilterProductByUserRequest request)
		{
			var products = await _productService.GetAllProductsByUserId(request.UserId,request.Page,request.Limit);
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
		public async Task<IActionResult> GetProductDetailById(Guid id)
		{
			var productDetail = await _productService.GetProductDetailByIdAsync(id);
			if (productDetail == null)
			{
				return NotFound("Product not found.");
			}
			return Ok(productDetail);
		}

		[HttpPut("checked")]
		public async Task<IActionResult> UpdateProductCheckedStatus([FromBody] CheckedProductRequest request)
		{
			var result = await _productService.UpdateCheckedProductAtRecycler(request.PackageId, request.ProductQrCode);
			if (!result)
			{
				return BadRequest("Failed to update product checked status.");
			}
			return Ok(new { message = "Product checked status updated successfully." });
		}
		[HttpGet("admin/filter")]
		public async Task<IActionResult> AdminFilterProducts([FromQuery] AdminFilterProductRequest request)
		{
			var model = new AdminFilterProductModel
			{
				FromDate = request.FromDate,
				ToDate = request.ToDate,
				Page = request.Page,
				Limit = request.Limit,
				CategoryName = request.CategoryName,
				CollectionCompanyId = request.CollectionCompanyId,
			};
			var result = await _productService.AdminGetProductsAsync(model);
			return Ok(result);
		}
		[HttpPut("cancel/{productId}")]
		public async Task<IActionResult> CancelProduct([FromRoute] Guid productId,[FromBody] CancelProductRequest request)
		{
			var result = await _productService.CancelProduct(productId, request.Reason);
			if (!result)
			{
				return BadRequest("Failed to cancel product.");
			}
			return Ok(new { message = "Product cancelled successfully." });

		}
		[HttpGet("user/my-pickups")]
		public async Task<IActionResult> GetMyPickupProducts([FromQuery] Guid userId, [FromQuery] DateOnly pickUpDate)
		{
			var result = await _productService.GetProductNeedToPickUp(userId, pickUpDate);
			return Ok(result);
		}
		[HttpPost("seeder-qrcode")]
		public async Task<IActionResult> SeederQrCodeInProduct([FromBody] SeederQrCodeInProductRequest request)
		{
			var result = await _productService.SeederQrCodeInProduct(request.ProductIds, request.QrCodes);
			if (!result)
			{
				return BadRequest("Failed to seeder QR codes in products.");
			}
			return Ok(new { message = "Seeder QR codes in products successfully." });
		}
	}
}
