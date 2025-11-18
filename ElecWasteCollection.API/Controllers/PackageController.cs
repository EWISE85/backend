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
		public PackageController(IPackageService packageService)
		{
			_packageService = packageService;
		}
		[HttpPost]
		public  IActionResult CreatePackage([FromBody] CreatePackageRequest newPackage)
		{
			if (newPackage == null)
			{
				return BadRequest("Invalid data.");
			}

			var model = new CreatePackageModel
			{
				PackageId = newPackage.PackageId,
				PackageName = newPackage.PackageName,
				SmallCollectionPointsId = newPackage.SmallCollectionPointsId,
				ProductsQrCode = newPackage.ProductsQrCode
			};
			var result =  _packageService.CreatePackageAsync(model);
			if (result == null)
			{
				return StatusCode(400, "An error occurred while creating the package.");
			}

			return Ok(new { message = "Package created successfully.", packageId = result });
		}
		[HttpGet("{packageId}")]
		public IActionResult GetPackageById(string packageId)
		{
			var package = _packageService.GetPackageById(packageId);
			if (package == null)
			{
				return NotFound("Package not found.");
			}
			return Ok(package);
		}

		[HttpGet("filter")]
		public IActionResult GetPackagesByQuery([FromQuery] PackageSearchQueryRequest query)
		{
			var model = new PackageSearchQueryModel
			{
				Limit = query.Limit,
				Page = query.Page,
				SmallCollectionPointsId = query.SmallCollectionPointsId,
				Status = query.Status
			};
			var packages = _packageService.GetPackagesByQuery(model);
			return Ok(packages);
		}
		[HttpPut("{packageId}/status")]
		public IActionResult SealedPackageStatus([FromRoute] string packageId)
		{
			var result = _packageService.UpdatePackageStatus(packageId, "Đã đóng thùng");
			if (!result)
			{
				return BadRequest("Failed to update package status.");
			}
			return Ok(new { message = "Package status updated successfully." });
		}
		[HttpPut("{packageId}")]
		public IActionResult UpdatePackage([FromRoute] string packageId, [FromBody] UpdatePackageRequest updatePackage)
		{
			if (updatePackage == null)
			{
				return BadRequest("Invalid data.");
			}

			var model = new UpdatePackageModel
			{
				PackageId = packageId,
				PackageName = updatePackage.PackageName,
				SmallCollectionPointsId = updatePackage.SmallCollectionPointsId,
				ProductsQrCode = updatePackage.ProductsQrCode
			};
			var result = _packageService.UpdatePackageAsync(model);
			if (!result)
			{
				return StatusCode(400, "An error occurred while updating the package.");
			}

			return Ok(new { message = "Package updated successfully." });
		}
	}
}
