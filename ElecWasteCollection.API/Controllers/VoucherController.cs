using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/voucher")]
	[ApiController]
	public class VoucherController : ControllerBase
	{
		private readonly IVoucherService _voucherService;

		public VoucherController(IVoucherService voucherService)
		{
			_voucherService = voucherService;
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

		[HttpGet("paged")]
		public async Task<IActionResult> GetPagedVouchers([FromQuery] VoucherQueryRequest request)
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
			
			var result = await _voucherService.UserReceiveVoucher(request.UserId,request.VoucherId);
			if (result)
			{
				return Ok(new { Message = "Nhận voucher thành công" });
			}
			else
			{
				return BadRequest(new { Message = "Nhận voucher thất bại" });
			}
		}
	}
}
