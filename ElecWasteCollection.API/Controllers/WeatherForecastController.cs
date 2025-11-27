//using ElecWasteCollection.Application.IServices;
//using Microsoft.AspNetCore.Mvc;

//namespace ElecWasteCollection.API.Controllers
//{
//	[ApiController]
//	[Route("[controller]")]
//	public class WeatherForecastController : ControllerBase
//	{
//		private readonly IImageComparisonService _test;

//		private static readonly string[] Summaries = new[]
//		{
//			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
//		};

//		private readonly ILogger<WeatherForecastController> _logger;

//		public WeatherForecastController(ILogger<WeatherForecastController> logger, IImageComparisonService imageComparisonService)
//		{
//			_logger = logger;
//			_test = imageComparisonService;
//		}

//		[HttpGet(Name = "GetWeatherForecast")]
//		public IEnumerable<WeatherForecast> Get()
//		{
//			return Enumerable.Range(1, 5).Select(index => new WeatherForecast
//			{
//				Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//				TemperatureC = Random.Shared.Next(-20, 55),
//				Summary = Summaries[Random.Shared.Next(Summaries.Length)]
//			})
//			.ToArray();
//		}
//		[HttpPost]
//		public async Task<IActionResult> TestImageComparison([FromBody] test test)
//		{
//			var result = await _test.CompareImageSimilarityAsync(test.Image1, test.Image2);
//			return Ok(new { AreSimilar = result });
//		}
//	}
//	public class test
//	{
//		public string Image1 { get; set; }
//		public string Image2 { get; set; }
//	}
//}
