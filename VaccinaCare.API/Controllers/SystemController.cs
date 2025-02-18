using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;

namespace VaccinaCare.API.Controllers
{
    [ApiController]
    [Route("api/system")]
    public class SystemController : ControllerBase
    {
        private readonly VaccinaCareDbContext _context;
        private readonly ILoggerService _logger;

        public SystemController(VaccinaCareDbContext context, ILoggerService logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("seed-all-data")]
        [ProducesResponseType(typeof(ApiResult<object>), 200)]
        [ProducesResponseType(typeof(ApiResult<object>), 400)]
        [ProducesResponseType(typeof(ApiResult<object>), 500)]
        public async Task<IActionResult> SeedData()
        {
            try
            {
                await ClearDatabase(_context);

                // Seed data
                var roles = await SeedRoles();
                var users = await SeedUsers();
                var vaccines = await SeedVaccines();
                var policies = await SeedPolicies();

                return Ok(ApiResult<object>.Success(new
                {
                    Message = "Data seeded successfully.",
                    TotalRoles = roles.Count,
                    TotalUsers = users.Count,
                    TotalVaccines = vaccines.Count,
                    TotalPolicies = policies.Count,
                    RoleNames = roles.Select(r => r.RoleName),
                    UserEmails = users.Select(u => u.Email),
                    PolicyNames = policies.Select(p => p.PolicyName)
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

        private async Task<List<CancellationPolicy>> SeedPolicies()
        {
            var policies = new List<CancellationPolicy>
            {
                new CancellationPolicy
                {
                    PolicyName = "Free Cancellation Before 24 Hours",
                    Description =
                        "Customers can cancel their appointment at least 24 hours in advance without any fees.",
                    CancellationDeadline = 24, // Hours before the appointment
                    PenaltyFee = 0
                },
                new CancellationPolicy
                {
                    PolicyName = "Cancellation Within 24 Hours",
                    Description =
                        "Cancelling within 24 hours before the appointment will incur a penalty of 10% of the total booking value.",
                    CancellationDeadline = 12,
                    PenaltyFee = 10 // Percentage of the penalty fee
                },
                new CancellationPolicy
                {
                    PolicyName = "Last-Minute Cancellation",
                    Description =
                        "Cancelling within 6 hours before the appointment will incur a penalty of 50% of the total booking value.",
                    CancellationDeadline = 6,
                    PenaltyFee = 50
                },
                new CancellationPolicy
                {
                    PolicyName = "No Cancellation Before Appointment",
                    Description =
                        "If the customer does not cancel before the appointment time, a 100% penalty fee of the total booking value will be applied.",
                    CancellationDeadline = 0,
                    PenaltyFee = 100
                }
            };

            // Check if data already exists to avoid duplication
            if (!_context.CancellationPolicies.Any())
            {
                await _context.CancellationPolicies.AddRangeAsync(policies);
                await _context.SaveChangesAsync();
            }

            return policies;
        }


        private async Task<List<Role>> SeedRoles()
        {
            var roles = new List<Role>
            {
                new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), RoleName = RoleType.Customer },
                new Role { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), RoleName = RoleType.Staff },
                new Role { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), RoleName = RoleType.Admin }
            };

            _logger.Info("Seeding roles...");
            await _context.Roles.AddRangeAsync(roles);
            await _context.SaveChangesAsync();
            _logger.Success("Roles seeded successfully.");
            return roles;
        }

        private async Task<List<User>> SeedUsers()
        {
            var passwordHasher = new PasswordHasher();
            var users = new List<User>
            {
                new User
                {
                    FullName = "Admin Phuc",
                    Email = "phucadmin@example.com",
                    Gender = true,
                    DateOfBirth = new DateTime(1985, 1, 1),
                    PhoneNumber = "0393734206",
                    PasswordHash = passwordHasher.HashPassword("AdminPassword@"),
                    RoleName = RoleType.Admin
                },
                new User
                {
                    FullName = "Admin Two",
                    Email = "admin2@example.com",
                    Gender = false,
                    DateOfBirth = new DateTime(1987, 2, 2),
                    PhoneNumber = "0987654321",
                    PasswordHash = passwordHasher.HashPassword("AdminPassword@"),
                    RoleName = RoleType.Admin
                },
                new User
                {
                    FullName = "Staff One",
                    Email = "staff1@example.com",
                    Gender = true,
                    DateOfBirth = new DateTime(1990, 3, 3),
                    PhoneNumber = "1122334455",
                    PasswordHash = passwordHasher.HashPassword("StaffPassword@"),
                    RoleName = RoleType.Staff
                },
                new User
                {
                    FullName = "Staff Two",
                    Email = "staff2@example.com",
                    Gender = false,
                    DateOfBirth = new DateTime(1992, 4, 4),
                    PhoneNumber = "5566778899",
                    PasswordHash = passwordHasher.HashPassword("StaffPassword@"),
                    RoleName = RoleType.Staff
                }
            };

            _logger.Info("Seeding users...");
            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();
            _logger.Success("Users seeded successfully.");
            return users;
        }

        private async Task<List<Vaccine>> SeedVaccines()
{
    var vaccines = new List<Vaccine>
    {
        new Vaccine { VaccineName = "BCG", Description = "Phòng lao", Type = "Vietnam", Price = 150000, AvoidChronic = true, AvoidAllergy = false },
        new Vaccine { VaccineName = "Hepatitis B", Description = "Viêm gan B", Type = "Vietnam", Price = 200000, AvoidChronic = false, AvoidAllergy = false },
        new Vaccine { VaccineName = "Pentaxim", Description = "Bạch hầu, Ho gà, Uốn ván, Bại liệt, Hib", Type = "France", Price = 795000, AvoidChronic = false, AvoidAllergy = true },
        new Vaccine { VaccineName = "Infanrix Hexa", Description = "6 trong 1 (DTP, Bại liệt, Hib, Viêm gan B)", Type = "Belgium", Price = 1015000, AvoidChronic = false, AvoidAllergy = true },
        new Vaccine { VaccineName = "Rotateq", Description = "Ngừa tiêu chảy do Rotavirus", Type = "USA", Price = 665000, AvoidChronic = true, AvoidAllergy = true },
        new Vaccine { VaccineName = "IPV", Description = "Bại liệt (tiêm)", Type = "Belgium", Price = 450000, AvoidChronic = false, AvoidAllergy = false },
        new Vaccine { VaccineName = "OPV", Description = "Bại liệt (uống)", Type = "Vietnam", Price = 100000, AvoidChronic = false, AvoidAllergy = false },
        new Vaccine { VaccineName = "Synflorix", Description = "Phế cầu khuẩn", Type = "Belgium", Price = 1045000, AvoidChronic = false, AvoidAllergy = true },
        new Vaccine { VaccineName = "Measles (MVVac)", Description = "Sởi đơn", Type = "Vietnam", Price = 396000, AvoidChronic = false, AvoidAllergy = true },
        new Vaccine { VaccineName = "MMR II", Description = "Sởi - Quai bị - Rubella", Type = "USA", Price = 445000, AvoidChronic = false, AvoidAllergy = true },
        new Vaccine { VaccineName = "Varivax", Description = "Thủy đậu", Type = "USA", Price = 1085000, AvoidChronic = true, AvoidAllergy = true },
        new Vaccine { VaccineName = "Havrix", Description = "Viêm gan A", Type = "UK", Price = 850000, AvoidChronic = false, AvoidAllergy = false },
        new Vaccine { VaccineName = "Ixiaro", Description = "Viêm não Nhật Bản", Type = "Austria", Price = 1300000, AvoidChronic = false, AvoidAllergy = true },
        new Vaccine { VaccineName = "Typhim Vi", Description = "Thương hàn", Type = "France", Price = 900000, AvoidChronic = false, AvoidAllergy = false },
        new Vaccine { VaccineName = "Verorab", Description = "Dại", Type = "France", Price = 950000, AvoidChronic = false, AvoidAllergy = true },
        new Vaccine { VaccineName = "Menactra", Description = "Viêm màng não mô cầu", Type = "USA", Price = 1750000, AvoidChronic = false, AvoidAllergy = true },
        new Vaccine { VaccineName = "DTP", Description = "Bạch hầu, Uốn ván, Ho gà", Type = "Vietnam", Price = 650000, AvoidChronic = false, AvoidAllergy = true },
        new Vaccine { VaccineName = "Prevnar 13", Description = "Phế cầu", Type = "USA", Price = 1400000, AvoidChronic = false, AvoidAllergy = true },
        new Vaccine { VaccineName = "Gardasil", Description = "HPV (Ngừa ung thư cổ tử cung)", Type = "USA", Price = 1790000, AvoidChronic = false, AvoidAllergy = false },
        new Vaccine { VaccineName = "Vaxigrip Tetra", Description = "Cúm mùa", Type = "France", Price = 356000, AvoidChronic = false, AvoidAllergy = true },
        new Vaccine { VaccineName = "Pfizer-BioNTech COVID-19", Description = "COVID-19 (5+)", Type = "USA", Price = 1200000, AvoidChronic = false, AvoidAllergy = true },
        new Vaccine { VaccineName = "Qdenga", Description = "Sốt xuất huyết", Type = "Germany", Price = 1390000, AvoidChronic = true, AvoidAllergy = false },
        new Vaccine { VaccineName = "Tetanus TT", Description = "Uốn ván", Type = "Vietnam", Price = 149000, AvoidChronic = false, AvoidAllergy = false },
        new Vaccine { VaccineName = "Dukoral", Description = "Dịch tả", Type = "Sweden", Price = 980000, AvoidChronic = false, AvoidAllergy = true }
    };

    _logger.Info("Seeding vaccines...");
    await _context.Vaccines.AddRangeAsync(vaccines);
    await _context.SaveChangesAsync();
    _logger.Success("Vaccines seeded successfully.");

    return vaccines;
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
                    () => context.Notifications.ExecuteDeleteAsync(),
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

                    // Delete VaccinePackageDetail first, then VaccinePackage
                    () => context.VaccinePackageDetails.ExecuteDeleteAsync(), // Child
                    () => context.VaccinePackages.ExecuteDeleteAsync(), // Parent

                    () => context.ServiceAvailabilities.ExecuteDeleteAsync(),
                    () => context.Vaccines.ExecuteDeleteAsync(),
                    () => context.Users.ExecuteDeleteAsync(),
                    () => context.Roles.ExecuteDeleteAsync(),
                };


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