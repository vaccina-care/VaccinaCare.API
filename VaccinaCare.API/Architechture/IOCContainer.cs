using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using VaccinaCare.Application.Implement;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Service;
using VaccinaCare.Application.Service.Common;
using VaccinaCare.Domain;
using VaccinaCare.Repository;
using VaccinaCare.Repository.Commons;
using VaccinaCare.Repository.Interfaces;
using VaccinaCare.Repository.Repositories;

namespace VaccinaCare.API.Architechture
{
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
                .AddEnvironmentVariables()
                .Build();

            //ĐỂ TRỐNG, SAU NÀY SẼ SETUP FIRE BASE, VNPAY, PAYOS SAU
            return services;
        }

        public static IServiceCollection SetupBusinessServicesLayer(this IServiceCollection services)
        {
            // Add application services
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ICurrentTime, CurrentTime>();
            services.AddScoped<IClaimsService, ClaimsService>();
            services.AddScoped<ILoggerService, LoggerService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAuthService, AuthService>();
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
                    new Microsoft.OpenApi.Models.OpenApiInfo { Title = "GoodsDesignAPI", Version = "v1" });
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
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:SecretKey"]))
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
            });

            return services;
        }
    }
}