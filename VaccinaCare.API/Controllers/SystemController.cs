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
                    Description = "Bạch hầu, ho gà, uốn ván, bại liệt và viêm màng não mủ, viêm phổi do Hib",
                    Type = "Pháp", Price = 795000
                },
                new Vaccine
                {
                    VaccineName = "Infanrix Hexa (6in1)",
                    Description = "Bạch hầu, ho gà, uốn ván, bại liệt, viêm màng não mủ, viêm phổi do Hib, viêm gan B",
                    Type = "Bỉ", Price = 1015000
                },
                new Vaccine
                {
                    VaccineName = "Rotateq", Description = "Tiêu chảy cấp do Rota virus", Type = "Mỹ", Price = 665000
                },
                new Vaccine
                {
                    VaccineName = "Synflorix", Description = "Các bệnh do phế cầu", Type = "Bỉ", Price = 1045000
                },
                new Vaccine { VaccineName = "BCG (lọ 1ml)", Description = "Lao", Type = "Việt Nam", Price = 155000 },
                new Vaccine
                {
                    VaccineName = "Gene Hbvax 1ml", Description = "Viêm gan B người lớn", Type = "Việt Nam",
                    Price = 220000
                },
                new Vaccine
                {
                    VaccineName = "Bexsero", Description = "Viêm màng não do não mô cầu nhóm B", Type = "Ý",
                    Price = 1750000
                },
                new Vaccine { VaccineName = "MVVac (Lọ 5ml)", Description = "Sởi", Type = "Việt Nam", Price = 396000 },
                new Vaccine
                {
                    VaccineName = "MMR II (3 in 1)", Description = "Sởi – Quai bị – Rubella", Type = "Mỹ",
                    Price = 445000
                },
                new Vaccine { VaccineName = "Varivax", Description = "Thủy đậu", Type = "Mỹ", Price = 1085000 },
                new Vaccine
                {
                    VaccineName = "Shingrix", Description = "Zona thần kinh (giời leo)", Type = "Bỉ", Price = 3890000
                },
                new Vaccine
                {
                    VaccineName = "Vaxigrip Tetra 0.5ml", Description = "Cúm", Type = "Pháp", Price = 356000
                },
                new Vaccine
                {
                    VaccineName = "Gardasil 0.5ml",
                    Description = "Ung thư cổ tử cung, ung thư hầu họng, sùi mào gà... do HPV (4 chủng)", Type = "Mỹ",
                    Price = 1790000
                },
                new Vaccine { VaccineName = "Qdenga", Description = "Sốt xuất huyết", Type = "Đức", Price = 1390000 },
                new Vaccine
                {
                    VaccineName = "Vắc xin uốn ván hấp phụ (TT)", Description = "Uốn ván", Type = "Việt Nam",
                    Price = 149000
                },
                new Vaccine
                {
                    VaccineName = "Imojev", Description = "Viêm não Nhật Bản", Type = "Thái Lan", Price = 875000
                },
                new Vaccine { VaccineName = "Verorab 0.5ml (TB)", Description = "Dại", Type = "Pháp", Price = 495000 },
                new Vaccine
                {
                    VaccineName = "Adacel", Description = "Bạch hầu – Uốn ván – Ho gà", Type = "Canada", Price = 775000
                },
                new Vaccine
                {
                    VaccineName = "Tetraxim", Description = "Bạch hầu – Ho gà – Uốn ván – Bại liệt", Type = "Pháp",
                    Price = 645000
                },
                new Vaccine
                {
                    VaccineName = "Havax 0.5ml", Description = "Viêm gan A", Type = "Việt Nam", Price = 255000
                },
                new Vaccine
                {
                    VaccineName = "Typhoid VI", Description = "Thương hàn", Type = "Việt Nam", Price = 265000
                },
                new Vaccine { VaccineName = "Morcvax", Description = "Tả", Type = "Việt Nam", Price = 165000 }
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