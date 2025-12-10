
using ElecWasteCollection.API.Hubs;
using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.Interfaces;

//using ElecWasteCollection.Application.Interfaces;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Services;
using ElecWasteCollection.Application.Services.AssignPostService;
using ElecWasteCollection.Domain.IRepository;
using ElecWasteCollection.Infrastructure.Context;
using ElecWasteCollection.Infrastructure.ExternalService;
using ElecWasteCollection.Infrastructure.ExternalService.Imagga;
using ElecWasteCollection.Infrastructure.Implementations;
using ElecWasteCollection.Infrastructure.Repository;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
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
			builder.Services.AddDbContext<ElecWasteCollectionDbContext>(opt =>
				opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
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
			builder.Services.AddScoped<IImageComparisonService, EmguImageQualityService>();
			builder.Services.AddScoped<IUserAddressService, UserAddressService>();
            builder.Services.AddScoped<ICompanyConfigService, CompanyConfigService>();
            builder.Services.AddScoped<IProductAssignService, ProductAssignService>();
            builder.Services.AddScoped<IProductQueryService, ProductQueryService>();

            builder.Services.AddHttpClient<MapboxDirectionsClient>();
            builder.Services.AddSingleton<IMapboxDistanceCacheService, MapboxDistanceCacheService>();
			builder.Services.AddHttpClient<MapboxMatrixClient>();

			builder.Services.AddScoped<IAttributeOptionService, AttributeOptionService>();
			builder.Services.AddScoped<IExcelImportService, ExcelImportService>();
			builder.Services.AddScoped<ICollectionCompanyService, CollectionCompanyService>();
			builder.Services.AddScoped<IAccountService, AccountService>();
			builder.Services.AddScoped<ISmallCollectionService, SmallCollectionService>();
			builder.Services.AddScoped<IImageRecognitionService, ImaggaImageService>();
			builder.Services.AddScoped<ISystemConfigService, SystemConfigService>();

            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

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
			builder.Services.Configure<ImaggaSettings>(builder.Configuration.GetSection("ImaggaAuth"));
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
			_ = FakeDataSeeder.collectionRoutes;
			_ = FakeDataSeeder.categories;
			_ = FakeDataSeeder.products;
			_ = FakeDataSeeder.productValues;
			_ = FakeDataSeeder.attributes;
			_ = FakeDataSeeder.categoryAttributes;
			_ = FakeDataSeeder.attributeOptions;
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
