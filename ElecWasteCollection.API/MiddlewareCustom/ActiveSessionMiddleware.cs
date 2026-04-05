using ElecWasteCollection.Domain.IRepository;
using System.Security.Claims;

namespace ElecWasteCollection.API.MiddlewareCustom
{
	public class ActiveSessionMiddleware
	{
		private readonly RequestDelegate _next;

		public ActiveSessionMiddleware(RequestDelegate next)
		{
			_next = next;
		}

		public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
		{
			if (context.User.Identity?.IsAuthenticated == true)
			{
				
				using var scope = serviceProvider.CreateScope();
				var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

				var userIdStr = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

				string authHeader = context.Request.Headers["Authorization"];
				string currentToken = authHeader?.Replace("Bearer ", "").Trim();

				if (Guid.TryParse(userIdStr, out Guid userId) && !string.IsNullOrEmpty(currentToken))
				{
					var userTokens = await unitOfWork.UserTokens.GetsAsync(t =>
						t.UserId == userId && t.AccessToken == currentToken);

					var activeToken = userTokens.FirstOrDefault();
					if (activeToken == null)
					{
						context.Response.StatusCode = StatusCodes.Status401Unauthorized;
						context.Response.ContentType = "application/json";

						var response = new {
							errorCode = "SESSION_TERMINATED",
							message = "Tài khoản đã đăng nhập ở thiết bị khác. Vui lòng đăng nhập lại." };
						await context.Response.WriteAsJsonAsync(response);
						return;
					}
				}
			}
			await _next(context);
		}
	}
}
