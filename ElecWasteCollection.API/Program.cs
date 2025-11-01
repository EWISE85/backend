
using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Services;
using ElecWasteCollection.Infrastructure.Implementations;

namespace ElecWasteCollection.API
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.

			builder.Services.AddControllers();
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();
			builder.Services.AddScoped<IPostService, PostService>();
			builder.Services.AddScoped<IUserService, UserService>();
			builder.Services.AddScoped<ICollectorService, CollectorService>();
			builder.Services.AddScoped<ICollectionRouteService, CollectionRouteService>();
			builder.Services.AddScoped<ICategoryService, CategoryService>();
			builder.Services.AddScoped<ISizeTierService, SizeTierService>();
			builder.Services.AddScoped<ICategoryAttributeService, CategoryAttributeService>();
			builder.Services.AddSingleton<IProfanityChecker, CustomProfanityChecker>();
			builder.Services.AddCors(options =>
			{
				options.AddPolicy("AllowAll", policy =>
				{
					policy.AllowAnyHeader()
						  .AllowAnyMethod()
						  .AllowCredentials()
						  .SetIsOriginAllowed(_ => true);
				});
			});
			var app = builder.Build();
			_ = FakeDataSeeder.users;
			_ = FakeDataSeeder.posts;
			_ = FakeDataSeeder.collector;
			_ = FakeDataSeeder.routes;
			_ = FakeDataSeeder.categories;
			_ = FakeDataSeeder.products;
			_ = FakeDataSeeder.productValues;
			_ = FakeDataSeeder.attributes;
			_ = FakeDataSeeder.sizeTiers;
			_ = FakeDataSeeder.categoryAttributes;
			app.UseCors("AllowAll");

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();

			app.UseAuthorization();


			app.MapControllers();

			app.Run();
		}
	}
}
