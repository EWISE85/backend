using ElecWasteCollection.Application.Exceptions;

namespace ElecWasteCollection.API.MiddlewareCustom
{
	public class HandlingException
	{
		private readonly RequestDelegate _next;
		private readonly ILogger<HandlingException> _logger;

		public HandlingException(RequestDelegate next, ILogger<HandlingException> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task Invoke(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (AppException ex)
			{
				context.Response.StatusCode = ex.StatusCode;
				await context.Response.WriteAsJsonAsync(new { message = ex.Message });
			}
			catch (Exception ex)
			{
				context.Response.StatusCode = 500;
				await context.Response.WriteAsJsonAsync(new { message = ex.Message });
			}
		}
	}
}
