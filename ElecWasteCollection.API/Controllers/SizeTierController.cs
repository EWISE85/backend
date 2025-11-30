//using ElecWasteCollection.Application.IServices;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;

//namespace ElecWasteCollection.API.Controllers
//{
//	[Route("api/size-tier")]
//	[ApiController]
//	public class SizeTierController : ControllerBase
//	{
//		private readonly ISizeTierService _sizeTierService;
//		public SizeTierController(ISizeTierService sizeTierService)
//		{
//			_sizeTierService = sizeTierService;
//		}
//		[HttpGet("{subCategoryId}")]
//		public IActionResult GetSizeTierBySubCategoryId([FromRoute] Guid subCategoryId)
//		{
//			var sizeTiers = _sizeTierService.GetAllSizeTierByCategoryId(subCategoryId);
//			return Ok(sizeTiers);
//		}
//	}
//}
