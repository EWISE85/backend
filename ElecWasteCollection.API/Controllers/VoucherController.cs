using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	//[Authorize]
	[Route("api/voucher")]
	[ApiController]
	public class VoucherController : ControllerBase
	{
		private readonly IVoucherService _voucherService;
		private readonly IExcelImportService _excelImportService;

		public VoucherController(IVoucherService voucherService, IExcelImportService excelImportService)
		{
			_voucherService = voucherService;
			_excelImportService = excelImportService;
		}

		[HttpPost]
		public async Task<IActionResult> CreateVoucher([FromBody] CreateVoucherRequest request)
		{
			var model = new CreateVoucherModel
			{
				Code = request.Code,
				Name = request.Name,
				Description = request.Description,
				ImageUrl = request.ImageUrl,
				StartAt = request.StartAt,
				EndAt = request.EndAt,
				Value = request.Value,
				Quantity = request.Quantity,
				PointsToRedeem = request.PointsToRedeem
			};
			var result = await _voucherService.CreateVoucher(model);
			if (result)
			{
				return Ok(new { Message = "Tạo voucher thành công" });
			}
			else
			{
				return BadRequest(new { Message = "Tạo voucher thất bại" });
			}
		}
		[HttpPut("{voucherId}")]
		public async Task<IActionResult> UpdateVoucher([FromBody] CreateVoucherRequest request, [FromRoute] Guid voucherId)
		{
			var model = new CreateVoucherModel
			{
				Code = request.Code,
				Name = request.Name,
				Description = request.Description,
				ImageUrl = request.ImageUrl,
				StartAt = request.StartAt,
				EndAt = request.EndAt,
				Value = request.Value,
				Quantity = request.Quantity,
				PointsToRedeem = request.PointsToRedeem,
				Status = request.Status
			};
			var result = await _voucherService.UpdateVoucher(model, voucherId);
			if (result)
			{
				return Ok(new { Message = "Cập nhật voucher thành công" });
			}
			else
			{
				return BadRequest(new { Message = "Cập nhật voucher thất bại" });
			}
		}
		[HttpGet("{id}")]
		public async Task<IActionResult> GetVoucherById([FromRoute] Guid id)
		{
			var result = await _voucherService.GetVoucherById(id);
			if (result == null)
			{
				return NotFound();
			}
			return Ok(result);
		}

		[HttpGet("{userId}/paged")]
		public async Task<IActionResult> GetPagedVouchers([FromQuery] VoucherQueryRequest request, [FromRoute] Guid userId)
		{
			var model = new VoucherQueryModel
			{
				Name = request.Name,
				Status = request.Status,
				PageNumber = request.Page,
				Limit = request.Limit
			};
			var result = await _voucherService.GetPagedVouchersForUser(model, userId);
			return Ok(result);
		}
		[HttpGet("admin/paged")]
		public async Task<IActionResult> GetPagedVouchersForAdmin([FromQuery] VoucherQueryRequest request)
		{
			var model = new VoucherQueryModel
			{
				Name = request.Name,
				Status = request.Status,
				PageNumber = request.Page,
				Limit = request.Limit
			};
			var result = await _voucherService.GetPagedVouchers(model);
			return Ok(result);
		}
		[HttpGet("user/{userId}/paged")]
		public async Task<IActionResult> GetPagedVouchersByUser([FromRoute] Guid userId, [FromQuery] VoucherQueryRequest request)
		{
			var model = new UserVoucherQueryModel
			{
				UserId = userId,
				Name = request.Name,
				Status = request.Status,
				PageNumber = request.Page,
				Limit = request.Limit
			};
			var result = await _voucherService.GetPagedVouchersByUser(model);
			return Ok(result);
		}

		[HttpPost("user/receive-voucher")]
		public async Task<IActionResult> ReceiveVoucher([FromBody] UserReceiveVoucherRequest request)
		{

			var result = await _voucherService.UserReceiveVoucher(request.UserId, request.VoucherId);
			if (result)
			{
				return Ok(new { Message = "Nhận voucher thành công" });
			}
			else
			{
				return BadRequest(new { Message = "Nhận voucher thất bại" });
			}
		}

		[HttpPost("import-format")]
		public async Task<IActionResult> ImportFormatExcel([FromForm] ImportFormatExcelRequest request)
		{
			if (request.FormFile == null || request.FormFile.Length == 0)
			{
				return BadRequest(new { Message = "Vui lòng chọn file Excel để tải lên." });
			}
			try
			{
				await _voucherService.UpdateFormatExcel(request.SystemConfigId, request.FormFile);
				return Ok(new { Message = "Cập nhật định dạng Excel thành công." });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { Message = "Lỗi khi cập nhật định dạng Excel.", Error = ex.Message });
			}
		}

		[HttpPost("import-excel")]
		public async Task<IActionResult> ImportVouchersFromExcel(IFormFile request)
		{

			try
			{
				using var stream = request.OpenReadStream();
				var result = await _excelImportService.ImportAsync(stream, "Voucher");
				if (result.Success)
				{
					return Ok(new { Message = "Import vouchers thành công.", Details = result.Messages });
				}
				else
				{
					return BadRequest(new { Message = "Import vouchers thất bại.", Details = result.Messages });
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { Message = "Lỗi khi import vouchers từ Excel.", Error = ex.Message });
			}
		}
		[HttpPatch("unactive/{voucherId}")]
		public async Task<IActionResult> UnactiveVoucher([FromRoute] Guid voucherId)
		{
			var result = await _voucherService.UnActiveVoucher(voucherId);
			if (result)
			{
				return Ok(new { Message = "Hủy kích hoạt voucher thành công" });
			}
			else
			{
				return BadRequest(new { Message = "Hủy kích hoạt voucher thất bại" });
			}
		}
		[HttpPatch("active/{voucherId}")]
		public async Task<IActionResult> ActiveVoucher([FromRoute] Guid voucherId)
		{
			var result = await _voucherService.ActiveVoucher(voucherId);
			if (result)
			{
				return Ok(new { Message = "Kích hoạt voucher thành công" });
			}
			else
			{
				return BadRequest(new { Message = "Kích hoạt voucher thất bại" });
			}
		}

	}
}
