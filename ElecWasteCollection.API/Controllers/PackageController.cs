using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/packages/")]
	[ApiController]
	public class PackageController : ControllerBase
	{
		private readonly IPackageService _packageService;
		private const string DANG_VAN_CHUYEN = "Đang vận chuyển";
		private const string TAI_CHE = "Tái chế";
		private const string DA_DONG_THUNG = "Đã đóng thùng";
		public PackageController(IPackageService packageService)
		{
			_packageService = packageService;
		}
		[HttpPost]
		public async Task<IActionResult> CreatePackage([FromBody] CreatePackageRequest newPackage)
		{
			if (newPackage == null)
			{
				return BadRequest("Invalid data.");
			}

			var model = new CreatePackageModel
			{
				PackageId = newPackage.PackageId,
				SmallCollectionPointsId = newPackage.SmallCollectionPointsId,
				ProductsQrCode = newPackage.ProductsQrCode
			};
			var result =  await _packageService.CreatePackageAsync(model);
			if (result == null)
			{
				return StatusCode(400, "An error occurred while creating the package.");
			}

			return Ok(new { message = "Package created successfully.", packageId = result });
		}
		[HttpGet("{packageId}")]
		public async Task<IActionResult> GetPackageById(string packageId)
		{
			var package = await _packageService.GetPackageById(packageId);
			if (package == null)
			{
				return NotFound("Package not found.");
			}
			return Ok(package);
		}

		[HttpGet("filter")]
		public async Task<IActionResult> GetPackagesByQuery([FromQuery] PackageSearchQueryRequest query)
		{
			var model = new PackageSearchQueryModel
			{
				Limit = query.Limit,
				Page = query.Page,
				SmallCollectionPointsId = query.SmallCollectionPointsId,
				Status = query.Status
			};
			var packages = await _packageService.GetPackagesByQuery(model);
			return Ok(packages);
		}
		[HttpGet("recycler/filter")]
		public async Task<IActionResult> GetPackagesRecyclerByQuery([FromQuery] PackageSearchRecyclerQueryRequest query)
		{
			var model = new PackageRecyclerSearchQueryModel
			{
				Limit = query.Limit,
				Page = query.Page,
				RecyclerCompanyId = query.RecyclerId,
				Status = query.Status
			};
			var packages = await _packageService.GetPackagesByRecylerQuery(model);
			return Ok(packages);
		}
		[HttpPut("{packageId}/status")]
		public async Task<IActionResult> SealedPackageStatus([FromRoute] string packageId)
		{
			var result = await _packageService.UpdatePackageStatus(packageId, DA_DONG_THUNG);
			if (!result)
			{
				return BadRequest("Failed to update package status.");
			}
			return Ok(new { message = "Package status updated successfully." });
		}
		[HttpPut("{packageId}")]
		public async Task<IActionResult> UpdatePackage([FromRoute] string packageId, [FromBody] UpdatePackageRequest updatePackage)
		{
			if (updatePackage == null)
			{
				return BadRequest("Invalid data.");
			}

			var model = new UpdatePackageModel
			{
				PackageId = packageId,
				SmallCollectionPointsId = updatePackage.SmallCollectionPointsId,
				ProductsQrCode = updatePackage.ProductsQrCode
			};
			var result = await _packageService.UpdatePackageAsync(model);
			if (!result)
			{
				return StatusCode(400, "An error occurred while updating the package.");
			}

			return Ok(new { message = "Package updated successfully." });
		}

		[HttpGet("delivery")]
		public async Task<IActionResult> GetPackagesWhenDelivery()
		{
			var packages = await _packageService.GetPackagesWhenDelivery();
			return Ok(packages);
		}
		[HttpPut("{packageId}/delivery")]
		public async Task<IActionResult> UpdatePackageStatusToDelivering([FromRoute] string packageId)
		{
			var result = await _packageService.UpdatePackageStatusDeliveryAndRecycler(packageId, DANG_VAN_CHUYEN);
			if (!result)
			{
				return BadRequest("Failed to update package status.");
			}
			return Ok(new { message = "Package status updated to 'Đang vận chuyển' successfully." });
		}
		[HttpPut("{packageId}/recycler")]
		public async Task<IActionResult> UpdatePackageStatusToRecycled([FromRoute] string packageId)
		{
			var result = await _packageService.UpdatePackageStatusDeliveryAndRecycler(packageId, TAI_CHE);
			if (!result)
			{
				return BadRequest("Failed to update package status.");
			}
			return Ok(new { message = "Package status updated successfully." });
		}
    }
}
