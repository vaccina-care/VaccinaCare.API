using Microsoft.EntityFrameworkCore;
using VaccinaCare.Application.Implement;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
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
            builder.WebHost.UseUrls("http://0.0.0.0:5000");
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<ICurrentTime, CurrentTime>();
            builder.Services.AddScoped<IClaimsService, ClaimsService>();
            
            builder.Services.AddHttpContextAccessor();

            
            builder.Services.AddScoped<ILoggerService, LoggerService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();

            builder.Services.AddControllers();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Configuration.AddEnvironmentVariables();

            builder.Services.AddDbContext<VaccinaCareDbContext>(options =>
            {
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("Connection string không được để trống!");
                }
                options.UseSqlServer(connectionString);
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

            // app.UseHttpsRedirection();
            app.UseRouting();

            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                app.ApplyMigrations(logger);
            }

            app.UseAuthorization();
            app.MapControllers();
        }
    }
}
