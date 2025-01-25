using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.API.Controllers
{
    [ApiController]
    [Route("api/system")]
    public class SystemController : ControllerBase
    {
        private readonly VaccinaCareDbContext _context;
        private readonly IAuthService _authService;
        private readonly ILoggerService _logger;

        public SystemController(VaccinaCareDbContext context, ILoggerService logger, IAuthService authService)
        {
            _context = context;
            _logger = logger;
            _authService = authService;
        }

        [HttpPost("seed-data")]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> SeedData()
        {
            try
            {
                // Xóa database trước khi seed
                await ClearDatabase(_context);

                // Seed roles
                var roles = GetDefaultRoles();
                foreach (var role in roles)
                {
                    if (!_context.Roles.Any(r => r.RoleName == role.RoleName))
                    {
                        _context.Roles.Add(role);
                    }
                }

                // Save changes sau khi thêm roles
                await _context.SaveChangesAsync();

                // Seed accounts
                var passwordHasher = new PasswordHasher();
                var accounts = new List<User>
                {
                    new User
                    {
                        FullName = "Admin Phuc",
                        Email = "phucadmin@example.com",
                        Gender = true,
                        DateOfBirth = new DateTime(1985, 1, 1),
                        PhoneNumber = "0393734206",
                        PasswordHash = passwordHasher.HashPassword("AdminPassword@"),
                        RoleId = roles.First(r => r.RoleName == "Admin").Id
                    },
                    new User
                    {
                        FullName = "Admin Two",
                        Email = "admin2@example.com",
                        Gender = false,
                        DateOfBirth = new DateTime(1987, 2, 2),
                        PhoneNumber = "0987654321",
                        PasswordHash = passwordHasher.HashPassword("AdminPassword@"),
                        RoleId = roles.First(r => r.RoleName == "Admin").Id
                    },
                    new User
                    {
                        FullName = "Staff One",
                        Email = "staff1@example.com",
                        Gender = true,
                        DateOfBirth = new DateTime(1990, 3, 3),
                        PhoneNumber = "1122334455",
                        PasswordHash = passwordHasher.HashPassword("StaffPassword@"),
                        RoleId = roles.First(r => r.RoleName == "Staff").Id
                    },
                    new User
                    {
                        FullName = "Staff Two",
                        Email = "staff2@example.com",
                        Gender = false,
                        DateOfBirth = new DateTime(1992, 4, 4),
                        PhoneNumber = "5566778899",
                        PasswordHash = passwordHasher.HashPassword("StaffPassword@"),
                        RoleId = roles.First(r => r.RoleName == "Staff").Id
                    }
                };

                await _context.Users.AddRangeAsync(accounts);
                await _context.SaveChangesAsync();

                // Trả kết quả
                return Ok(ApiResult<object>.Success(new
                {
                    Message = "Data seeded successfully.",
                    TotalRoles = roles.Count,
                    TotalAccounts = accounts.Count,
                    RoleNames = roles.Select(r => r.RoleName),
                    AccountEmails = accounts.Select(a => a.Email)
                }));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.Error($"Database update error: {dbEx.Message}");
                return StatusCode(500, "Error seeding data: Database issue.");
            }
            catch (Exception ex)
            {
                _logger.Error($"General error: {ex.Message}");
                return StatusCode(500, "Error seeding data: General failure.");
            }
        }

        
        //PRIVATE METHOD
        private List<Role> GetDefaultRoles()
        {
            return new List<Role>
            {
                new Role { RoleName = "Customer", CreatedBy = null },
                new Role { RoleName = "Admin", CreatedBy = null },
                new Role { RoleName = "Staff", CreatedBy = null }
            };
        }
        private async Task ClearDatabase(VaccinaCareDbContext context)
        {
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                _logger.Info("Bắt đầu xóa dữ liệu trong database...");

                // Danh sách các bảng cần xóa (theo thứ tự quan hệ FK)
                var tablesToDelete = new List<Func<Task>>
                {
                    () => context.Notifications.ExecuteDeleteAsync(), // Xóa bảng con trước
                    () => context.AppointmentsServices.ExecuteDeleteAsync(),
                    () => context.Appointments.ExecuteDeleteAsync(),
                    () => context.CancellationPolicies.ExecuteDeleteAsync(),
                    () => context.Children.ExecuteDeleteAsync(),
                    () => context.Feedbacks.ExecuteDeleteAsync(),
                    () => context.Invoices.ExecuteDeleteAsync(),
                    () => context.PackageProgresses.ExecuteDeleteAsync(),
                    () => context.Payments.ExecuteDeleteAsync(),
                    () => context.UsersVaccinationServices.ExecuteDeleteAsync(),
                    () => context.VaccinationRecords.ExecuteDeleteAsync(),
                    () => context.VaccineSuggestions.ExecuteDeleteAsync(),
                    () => context.VaccinePackageDetails.ExecuteDeleteAsync(),
                    () => context.VaccinePackages.ExecuteDeleteAsync(),
                    () => context.ServiceAvailabilities.ExecuteDeleteAsync(),
                    () => context.Services.ExecuteDeleteAsync(),
                    () => context.Users.ExecuteDeleteAsync(), // Xóa bảng cha sau cùng
                    () => context.Roles.ExecuteDeleteAsync(), // Xóa roles sau users nếu không có cascade delete
                };

                // Chạy các tác vụ xóa tuần tự
                foreach (var deleteFunc in tablesToDelete)
                {
                    await deleteFunc();
                }

                await transaction.CommitAsync();
                _logger.Success("Xóa sạch dữ liệu trong database thành công.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.Error($"Xóa dữ liệu thất bại: {ex.Message}");
                throw;
            }
        }
    }
}