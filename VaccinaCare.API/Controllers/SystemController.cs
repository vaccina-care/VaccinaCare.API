using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemController : ControllerBase
    {
        private readonly VaccinaCareDbContext _context;
        private readonly ILoggerService _logger;

        public SystemController(VaccinaCareDbContext context, ILoggerService logger)
        {
            _context = context;
            _logger = logger;
        }
        [HttpPost("seed-roles")]
        public async Task<IActionResult> SeedRoles()
        {
            try
            {
                // Clear the database before seeding roles
                await ClearDatabase(_context);

                // Seed roles
                var roles = new List<Role>
        {
            new Role { RoleName = "Customer", CreatedBy = null },
            new Role { RoleName = "Admin", CreatedBy = null },
            new Role { RoleName = "Staff", CreatedBy = null }
        };

                foreach (var role in roles)
                {
                    if (!_context.Roles.Any(r => r.RoleName == role.RoleName))
                    {
                        _context.Roles.Add(role);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(ApiResult<object>.Success(new
                {
                    Message = "Role data seeded successfully after clearing the database.",
                    Details = new
                    {
                        TotalSeededRoles = roles.Count,
                        SeededRoles = roles.Select(r => r.RoleName)
                    }
                }));
            }
            catch (Exception ex)
            {
                // Extract status code from exception message or default to 500
                int statusCode = ExceptionUtils.ExtractStatusCode(ex.Message);

                // Return standardized error response
                return StatusCode(statusCode, ExceptionUtils.CreateErrorResponse($"An error occurred while seeding roles: {ex.Message}"));
            }
        }




        private async Task ClearDatabase(VaccinaCareDbContext context)
        {
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                _logger.Info("Clearing all data in the database...");

                // Clear all tables
                await context.Users.ExecuteDeleteAsync();

                await context.Appointments.ExecuteDeleteAsync();
                await context.AppointmentsServices.ExecuteDeleteAsync();
                await context.CancellationPolicies.ExecuteDeleteAsync();
                await context.Children.ExecuteDeleteAsync();
                await context.Feedbacks.ExecuteDeleteAsync();
                await context.Invoices.ExecuteDeleteAsync();
                await context.Notifications.ExecuteDeleteAsync();
                await context.PackageProgresses.ExecuteDeleteAsync();
                await context.Payments.ExecuteDeleteAsync();
                await context.Roles.ExecuteDeleteAsync();
                await context.Services.ExecuteDeleteAsync();
                await context.ServiceAvailabilities.ExecuteDeleteAsync();
                await context.UsersVaccinationServices.ExecuteDeleteAsync();
                await context.VaccinationRecords.ExecuteDeleteAsync();
                await context.VaccinePackages.ExecuteDeleteAsync();
                await context.VaccinePackageDetails.ExecuteDeleteAsync();
                await context.VaccineSuggestions.ExecuteDeleteAsync();

                // Commit transaction
                await transaction.CommitAsync();
                _logger.Success("All data cleared successfully.");
            }
            catch (Exception ex)
            {
                // Rollback transaction
                await transaction.RollbackAsync();
                _logger.Error($"Failed to clear database: {ex.Message}");
                throw;
            }
        }



        // Endpoint không cần authen
        [HttpGet("public")]
        public IActionResult PublicEndpoint()
        {
            return Ok("Đây là endpoint công khai, không cần authen.");
        }

        // Test endpoint for Customer users
        [HttpGet("customer")]
        [Authorize(Policy = "CustomerPolicy")] // Requires Customer role
        public IActionResult GetCustomerContent()
        {
            return Ok(new { Message = "Welcome, Customer! You are authorized to access this endpoint." });
        }


    }
}
