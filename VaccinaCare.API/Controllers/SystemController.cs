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

                // seed data
                var roles = await SeedRoles();
                var users = await SeedUsers();
                var vaccines = await SeedVaccines();

                return Ok(ApiResult<object>.Success(new
                {
                    Message = "Data seeded successfully.",
                    TotalRoles = roles.Count,
                    TotalUsers = users.Count,
                    TotalVaccines = vaccines.Count,
                    RoleNames = roles.Select(r => r.RoleName),
                    UserEmails = users.Select(u => u.Email)
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
                new Vaccine
                {
                    VaccineName = "Pentaxim",
                    Description = "Diphtheria, Pertussis, Tetanus, Poliomyelitis, and Haemophilus influenzae type B",
                    Type = "France",
                    Price = 795000,
                    ForBloodType = BloodType.Unknown,
                    AvoidChronic = false,
                    AvoidAllergy = true, // Contains pertussis component, may trigger allergic reactions
                    HasDrugInteraction = false,
                    HasSpecialWarning = false
                },
                new Vaccine
                {
                    VaccineName = "Infanrix Hexa (6in1)",
                    Description =
                        "Diphtheria, Pertussis, Tetanus, Poliomyelitis, Haemophilus influenzae type B, and Hepatitis B",
                    Type = "Belgium",
                    Price = 1015000,
                    ForBloodType = BloodType.Unknown,
                    AvoidChronic = false,
                    AvoidAllergy = true, // Contains multiple components that may trigger allergies
                    HasDrugInteraction = false,
                    HasSpecialWarning = false
                },
                new Vaccine
                {
                    VaccineName = "Rotateq",
                    Description = "Prevention of severe diarrhea caused by Rotavirus",
                    Type = "USA",
                    Price = 665000,
                    ForBloodType = BloodType.Unknown,
                    AvoidChronic = true, // Not recommended for immunocompromised individuals
                    AvoidAllergy = true, // Contains live virus, may cause allergic reactions
                    HasDrugInteraction = false,
                    HasSpecialWarning = true // Special precaution for immunodeficient individuals
                },
                new Vaccine
                {
                    VaccineName = "Synflorix",
                    Description = "Prevention of pneumococcal infections",
                    Type = "Belgium",
                    Price = 1045000,
                    ForBloodType = BloodType.Unknown,
                    AvoidChronic = false,
                    AvoidAllergy = true, // May trigger reactions in individuals allergic to diphtheria toxoid
                    HasDrugInteraction = false,
                    HasSpecialWarning = false
                },
                new Vaccine
                {
                    VaccineName = "BCG (1ml vial)",
                    Description = "Prevention of tuberculosis",
                    Type = "Vietnam",
                    Price = 155000,
                    ForBloodType = BloodType.Unknown,
                    AvoidChronic = true, // Not recommended for immunocompromised individuals
                    AvoidAllergy = false,
                    HasDrugInteraction = true, // Can interfere with tuberculosis skin tests
                    HasSpecialWarning = true // Special precaution for individuals with HIV
                },
                new Vaccine
                {
                    VaccineName = "Gene Hbvax 1ml",
                    Description = "Hepatitis B (for adults)",
                    Type = "Vietnam",
                    Price = 220000,
                    ForBloodType = BloodType.Unknown,
                    AvoidChronic = false,
                    AvoidAllergy = false,
                    HasDrugInteraction = false,
                    HasSpecialWarning = false
                },
                new Vaccine
                {
                    VaccineName = "Bexsero",
                    Description = "Prevention of meningococcal disease caused by Neisseria meningitidis group B",
                    Type = "Italy",
                    Price = 1750000,
                    ForBloodType = BloodType.Unknown,
                    AvoidChronic = false,
                    AvoidAllergy = true, // Contains recombinant proteins, possible allergic reactions
                    HasDrugInteraction = false,
                    HasSpecialWarning = false
                },
                new Vaccine
                {
                    VaccineName = "MVVac (5ml vial)",
                    Description = "Measles",
                    Type = "Vietnam",
                    Price = 396000,
                    ForBloodType = BloodType.Unknown,
                    AvoidChronic = false,
                    AvoidAllergy = true, // Contains egg protein, potential allergy risk
                    HasDrugInteraction = false,
                    HasSpecialWarning = false
                },
                new Vaccine
                {
                    VaccineName = "MMR II (3 in 1)",
                    Description = "Measles, Mumps, and Rubella",
                    Type = "USA",
                    Price = 445000,
                    ForBloodType = BloodType.Unknown,
                    AvoidChronic = false,
                    AvoidAllergy = true, // Contains egg and gelatin, potential allergy risk
                    HasDrugInteraction = false,
                    HasSpecialWarning = false
                },
                new Vaccine
                {
                    VaccineName = "Varivax",
                    Description = "Chickenpox (Varicella)",
                    Type = "USA",
                    Price = 1085000,
                    ForBloodType = BloodType.Unknown,
                    AvoidChronic = true, // Not recommended for immunocompromised individuals
                    AvoidAllergy = true, // Contains gelatin, egg, and neomycin
                    HasDrugInteraction = false,
                    HasSpecialWarning = true // Special precaution for immunodeficient individuals
                },
                new Vaccine
                {
                    VaccineName = "Shingrix",
                    Description = "Shingles (Herpes Zoster)",
                    Type = "Belgium",
                    Price = 3890000,
                    ForBloodType = BloodType.Unknown,
                    AvoidChronic = false,
                    AvoidAllergy = true, // May cause allergic reactions in some individuals
                    HasDrugInteraction = false,
                    HasSpecialWarning = false
                },
                new Vaccine
                {
                    VaccineName = "Vaxigrip Tetra 0.5ml",
                    Description = "Influenza",
                    Type = "France",
                    Price = 356000,
                    ForBloodType = BloodType.Unknown,
                    AvoidChronic = false,
                    AvoidAllergy = true, // Contains egg protein, potential allergy risk
                    HasDrugInteraction = false,
                    HasSpecialWarning = false
                },
                new Vaccine
                {
                    VaccineName = "Gardasil 0.5ml",
                    Description = "HPV vaccine (Cervical cancer, throat cancer, genital warts)",
                    Type = "USA",
                    Price = 1790000,
                    ForBloodType = BloodType.Unknown,
                    AvoidChronic = false,
                    AvoidAllergy = false,
                    HasDrugInteraction = false,
                    HasSpecialWarning = false
                },
                new Vaccine
                {
                    VaccineName = "Qdenga",
                    Description = "Dengue fever prevention",
                    Type = "Germany",
                    Price = 1390000,
                    ForBloodType = BloodType.Unknown,
                    AvoidChronic = true, // Not recommended for individuals with severe immunodeficiency
                    AvoidAllergy = false,
                    HasDrugInteraction = false,
                    HasSpecialWarning = true // Special warning for individuals who have not had dengue before
                },
                new Vaccine
                {
                    VaccineName = "Tetanus Adsorbed Vaccine (TT)",
                    Description = "Tetanus",
                    Type = "Vietnam",
                    Price = 149000,
                    ForBloodType = BloodType.Unknown,
                    AvoidChronic = false,
                    AvoidAllergy = false,
                    HasDrugInteraction = false,
                    HasSpecialWarning = false
                }
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
                    () => context.Vaccines.ExecuteDeleteAsync(),
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