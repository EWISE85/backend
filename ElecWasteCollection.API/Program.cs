
using ElecWasteCollection.API.Helper;
using ElecWasteCollection.API.Hubs;
using ElecWasteCollection.API.MiddlewareCustom;
using ElecWasteCollection.Application.Interfaces;

//using ElecWasteCollection.Application.Interfaces;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Services;
using ElecWasteCollection.Application.Services.AssignPackageService;
using ElecWasteCollection.Application.Services.AssignPostService;
using ElecWasteCollection.Domain.IRepository;
using ElecWasteCollection.Infrastructure.Configuration;
using ElecWasteCollection.Infrastructure.Context;
using ElecWasteCollection.Infrastructure.ExternalService;
using ElecWasteCollection.Infrastructure.ExternalService.Apple;
using ElecWasteCollection.Infrastructure.ExternalService.Cloudinary;
using ElecWasteCollection.Infrastructure.ExternalService.Email;
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
			builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.Converters.Add(new VietnamDateTimeJsonConverter());
	});
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
			builder.Services.AddScoped<ICompanyService, CompanyService>();
			builder.Services.AddScoped<IAccountService, AccountService>();
			builder.Services.AddScoped<ISmallCollectionService, SmallCollectionService>();
			builder.Services.AddScoped<IImageRecognitionService, ImaggaImageService>();
			builder.Services.AddScoped<ISystemConfigService, SystemConfigService>();
			builder.Services.AddScoped<IShiftService, ShiftService>();
			builder.Services.AddScoped<IVehicleService, VehicleService>();
			builder.Services.AddScoped<IReassignDriverService, ReassignDriverService>();
			builder.Services.AddScoped<IPackageAssignService, PackageAssignService>();
            builder.Services.AddScoped<IRecyclingQueryService, RecyclingQueryService>();



            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
			builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
			builder.Services.AddScoped<IAccountRepsitory, AccountRepsitory>();
			builder.Services.AddScoped<IAttributeOptionRepository, AttributeOptionRepository>();
			builder.Services.AddScoped<IAttributeRepository, AttributeRepository>();
			builder.Services.AddScoped<IBrandRepository, BrandRepository>();
			builder.Services.AddScoped<ICategoryAttributeRepsitory, CategoryAttributeRepsitory>();
			builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
			builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
			builder.Services.AddScoped<ICollectionRouteRepository, CollectionRouteRepository>();
			builder.Services.AddScoped<ICollectorRepository, CollectorRepository>();
			builder.Services.AddScoped<IPackageRepository, PackageRepository>();
			builder.Services.AddScoped<IPointTransactionRepository, PointTransactionRepository>();
			builder.Services.AddScoped<IPostRepository, PostRepository>();
			builder.Services.AddScoped<IProductImageRepository, ProductImageRepository>();
			builder.Services.AddScoped<IProductRepository, ProductRepository>();
			builder.Services.AddScoped<IProductStatusHistoryRepository, ProductStatusHistoryRepository>();
			builder.Services.AddScoped<IProductValuesRepository, ProductValuesRepository>();
			builder.Services.AddScoped<IShiftRepository, ShiftRepository>();
			builder.Services.AddScoped<ISmallCollectionRepository, SmallCollectionRepository>();
			builder.Services.AddScoped<IUserAddressRepository, UserAddressRepository>();
			builder.Services.AddScoped<IUserPointRepository, UserPointRepository>();
			builder.Services.AddScoped<IUserRepository, UserRepository>();
			builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
			builder.Services.AddScoped<DbContext, ElecWasteCollectionDbContext>();
			builder.Services.AddScoped<ITrackingRepository, TrackingRepository>();
			builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
			builder.Services.AddScoped<IEmailService, EmailService>();
			builder.Services.AddScoped<IForgotPasswordService, ForgotPasswordService>();
			builder.Services.AddScoped<IForgotPasswordRepository, ForgotPasswordRepository>();
			builder.Services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();
			builder.Services.AddScoped<IAppleAuthService, AppleAuthService>();
			builder.Services.AddScoped<IDashboardService, DashboardService>();
			builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("Cloudinary"));
			builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
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
			builder.Services.Configure<AppleAuthSettings>(builder.Configuration.GetSection("AppleAuthSettings"));
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




			app.UseCors("AllowAll");

			// Configure the HTTP request pipeline.
			if (app.Environment.IsDevelopment())
			{
				app.UseSwagger();
				app.UseSwaggerUI();
			}

			app.UseHttpsRedirection();
			app.UseMiddleware<HandlingException>();
			app.UseAuthentication();
			app.UseAuthorization();

			app.MapHub<ShippingHub>("/shippingHub");
			app.MapControllers();

			app.Run();
		}
	}
}
