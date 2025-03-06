using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Service;
using VaccinaCare.Application.Service.Common;
using VaccinaCare.Domain;
using VaccinaCare.Repository;
using VaccinaCare.Repository.Commons;
using VaccinaCare.Repository.Interfaces;
using VaccinaCare.Repository.Repositories;
using VNPAY.NET;

namespace VaccinaCare.API.Architechture;

public static class IOCContainer
{
    public static IServiceCollection SetupIOCContainer(this IServiceCollection services)
    {
        //Add Logger
        services.AddScoped<ILoggerService, LoggerService>();

        //Add Project Services
        services.SetupDBContext();
        services.SetupSwagger();

        //Add generic repositories
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        //Add business services
        services.SetupBusinessServicesLayer();

        services.SetupCORS();
        services.SetupJWT();

        services.SetupThirdParty();
        return services;
    }


    public static IServiceCollection SetupThirdParty(this IServiceCollection services)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true)
            .AddEnvironmentVariables() // Đọc biến môi trường từ Docker
            .Build();

        var _tmnCode = configuration["Payment:VnPay:TmnCode"];
        var _hashSecret = configuration["Payment:VnPay:HashSecret"];
        var _baseUrl = configuration["Payment:VnPay:PaymentUrl"];
        var _callbackUrl = configuration["Payment:VnPay:ReturnUrl"];

        if (string.IsNullOrEmpty(_tmnCode) || string.IsNullOrEmpty(_hashSecret) ||
            string.IsNullOrEmpty(_baseUrl) || string.IsNullOrEmpty(_callbackUrl))
            throw new Exception("Không tìm thấy BaseUrl, TmnCode, HashSecret, hoặc CallbackUrl từ config.");

        var vnpay = new Vnpay();
        vnpay.Initialize(_tmnCode, _hashSecret, _baseUrl, _callbackUrl);
        return services.AddSingleton<IVnpay>(vnpay);
    }


    public static IServiceCollection SetupBusinessServicesLayer(this IServiceCollection services)
    {
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        // Add application services
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICurrentTime, CurrentTime>();
        services.AddScoped<IClaimsService, ClaimsService>();
        services.AddScoped<ILoggerService, LoggerService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IVaccineService, VaccineService>();
        services.AddScoped<IChildService, ChildService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IVaccineSuggestionService, VaccineSuggestionService>();
        services.AddScoped<IBlobService, BlobService>();
        services.AddScoped<IVaccinePackageService, VaccinePackageService>();
        services.AddScoped<IVaccineIntervalRulesService, VaccineIntervalRulesService>();
        services.AddScoped<IVaccineRecordService, VaccineRecordService>();
        services.AddScoped<IFeedbackService, FeedbackService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IVnpay, Vnpay>();
        services.AddHttpContextAccessor();

        return services;
    }


    private static IServiceCollection SetupDBContext(this IServiceCollection services)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true)
            .AddEnvironmentVariables()
            .Build();

        services.AddDbContext<VaccinaCareDbContext>(options =>
        {
            options.UseSqlServer(configuration["ConnectionStrings:DefaultConnection"]);
        });

        return services;
    }

    private static IServiceCollection SetupCORS(this IServiceCollection services)
    {
        services.AddCors(opt =>
        {
            opt.AddPolicy("CorsPolicy",
                policy => { policy.WithOrigins("*").AllowAnyHeader().AllowAnyMethod(); });
        });

        return services;
    }


    private static IServiceCollection SetupSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.UseInlineDefinitionsForEnums();

            c.SwaggerDoc("v1",
                new OpenApiInfo { Title = "VaccinaCareAPI", Version = "v1" });
            var jwtSecurityScheme = new OpenApiSecurityScheme
            {
                Name = "JWT Authentication",
                Description = "Enter your JWT token in this field",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            c.AddSecurityDefinition("Bearer", jwtSecurityScheme);

            var securityRequirement = new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            };

            c.AddSecurityRequirement(securityRequirement);

            // Cấu hình Swagger để sử dụng Newtonsoft.Json
            c.UseAllOfForInheritance();
        });


        return services;
    }

    private static IServiceCollection SetupJWT(this IServiceCollection services)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", true, true)
            .AddEnvironmentVariables()
            .Build();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, // Bật kiểm tra Issuer
                    ValidateAudience = true, // Bật kiểm tra Audience
                    ValidateLifetime = true,
                    ValidIssuer = configuration["JWT:Issuer"],
                    ValidAudience = configuration["JWT:Audience"],
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"]))
                };
            });
        services.AddAuthorization(options =>
        {
            options.AddPolicy("CustomerPolicy", policy =>
                policy.RequireRole("Customer"));

            options.AddPolicy("AdminPolicy", policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy("StaffPolicy", policy =>
                policy.RequireRole("Staff"));

            options.AddPolicy("AdminOrStaffPolicy", policy =>
                policy.RequireRole("Admin", "Staff"));
        });


        return services;
    }
}