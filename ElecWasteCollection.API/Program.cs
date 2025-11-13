
using ElecWasteCollection.API.Hubs;
using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.Interfaces;

//using ElecWasteCollection.Application.Interfaces;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Services;
using ElecWasteCollection.Infrastructure.ExternalService;
using ElecWasteCollection.Infrastructure.Implementations;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

namespace ElecWasteCollection.API
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddSignalR();
			builder.Services.AddControllers();
			// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(c =>
			{
				c.AddSecurityDefinition("JWT", new OpenApiSecurityScheme
				{
					Description = "Paste your JWT token (no need to include 'Bearer ')",
					Name = "Authorization",
					In = ParameterLocation.Header,
					Type = SecuritySchemeType.Http,
					Scheme = "bearer",
					BearerFormat = "JWT"
				});

				c.AddSecurityRequirement(new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme
						{
							Reference = new OpenApiReference
							{
								Type = ReferenceType.SecurityScheme,
								Id = "JWT"
							}
						},
						Array.Empty<string>()
					}
				});
			});
			if (FirebaseApp.DefaultInstance == null)
			{
				FirebaseApp.Create(new AppOptions()
				{
					Credential = GoogleCredential.FromFile("elecWasteCollection.json")
				});
			}
			builder.Services.AddScoped<IPostService, PostService>();
			builder.Services.AddScoped<IUserService, UserService>();
			builder.Services.AddScoped<ICollectorService, CollectorService>();
			builder.Services.AddScoped<ICollectionRouteService, CollectionRouteService>();
			builder.Services.AddScoped<ICategoryService, CategoryService>();
			builder.Services.AddScoped<ISizeTierService, SizeTierService>();
			builder.Services.AddScoped<ICategoryAttributeService, CategoryAttributeService>();
			builder.Services.AddSingleton<IProfanityChecker, CustomProfanityChecker>();
			builder.Services.AddScoped<IGroupingService, GroupingService>();
			builder.Services.AddScoped<IProductService, ProductService>();
			builder.Services.AddScoped<ITrackingService, TrackingService>();
			builder.Services.AddScoped<IShippingNotifierService, SignalRShippingNotifier>();
			builder.Services.AddScoped<ITokenService, TokenService>();
			builder.Services.AddSingleton<IFirebaseService, FirebaseService>();
			builder.Services.AddScoped<IPackageService, PackageService>();
			builder.Services.AddScoped<IBrandService, BrandService>();
			builder.Services.AddScoped<IPointTransactionService, PointTransactionService>();
			builder.Services.AddScoped<IUserPointService, UserPointService>();
			builder.Services.AddScoped<IImageComparisonService, ImageComparisonService>();
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
			var jwtSettings = builder.Configuration.GetSection("Jwt");
			var secretKey = jwtSettings["SecretKey"];
			builder.Services.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(options =>
			{
				var jwtSettings = builder.Configuration.GetSection("Jwt");
				var secretKey = jwtSettings["SecretKey"];
				var keyBytes = Encoding.UTF8.GetBytes(secretKey);

				options.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = jwtSettings["Issuer"],
					ValidAudience = jwtSettings["Audience"],
					IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
				};
			});
			var app = builder.Build();
			_ = FakeDataSeeder.users;
			_ = FakeDataSeeder.posts;
			_ = FakeDataSeeder.collectors;
			_ = FakeDataSeeder.collectionRoutes;
			_ = FakeDataSeeder.categories;
			_ = FakeDataSeeder.products;
			_ = FakeDataSeeder.productValues;
			_ = FakeDataSeeder.attributes;
			_ = FakeDataSeeder.sizeTiers;
			_ = FakeDataSeeder.categoryAttributes;
			_ = FakeDataSeeder.productImages;

			app.UseCors("AllowAll");

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();

			app.UseAuthorization();

			app.MapHub<ShippingHub>("/shippingHub");
			app.MapControllers();
			
			app.Run();
		}
	}
}
