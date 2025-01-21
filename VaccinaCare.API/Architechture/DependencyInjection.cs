using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VaccinaCare.Application.Implement;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Service;
using VaccinaCare.Application.Service.Common;
using VaccinaCare.Domain;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository;
using VaccinaCare.Repository.Commons;
using VaccinaCare.Repository.Interfaces;
using VaccinaCare.Repository.Repositories;

namespace VaccinaCare.API.Architechture
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Configures all services for the application.
        /// </summary>
        public static void ConfigureServices(this WebApplicationBuilder builder)
        {
            // Add Controllers with JSON options
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

            // Add configuration files and environment variables
            builder.Configuration
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            // Add JWT Authentication
            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["JWT:Issuer"], // Read from environment
                        ValidAudience = builder.Configuration["JWT:Audience"], // Read from environment
                        IssuerSigningKey =
                            new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(builder.Configuration["JWT:SecretKey"])) // Read from environment
                    };
                });


            // Add Authorization Policies
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
                options.AddPolicy("StaffPolicy", policy => policy.RequireRole("Staff"));
                options.AddPolicy("CustomerPolicy", policy => policy.RequireRole("Customer"));
            });

            // Add application services
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ICurrentTime, CurrentTime>();
            builder.Services.AddScoped<IClaimsService, ClaimsService>();
            builder.Services.AddScoped<ILoggerService, LoggerService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddHttpContextAccessor();

            // Configure database context
            builder.Services.AddDbContext<VaccinaCareDbContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Connection string không được để trống!");
                }

                options.UseSqlServer(connectionString);
            });

            // Add Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "VaccinaCare API", Version = "v1" });
            });
        }

        /// <summary>
        /// Configures the HTTP request pipeline for the application.
        /// </summary>
        public static void ConfigurePipeline(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "VaccinaCare API v1");
                    c.RoutePrefix = string.Empty; // Set Swagger UI at root
                });
            }

            // Uncomment for HTTPS redirection if needed
            // app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                app.ApplyMigrations(logger);
            }

            app.MapControllers();
        }
    }
}