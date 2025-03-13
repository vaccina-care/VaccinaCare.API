using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;

namespace VaccinaCare.API.Controllers;

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
    public async Task<IActionResult> SeedData()
    {
        try
        {
            await ClearDatabase(_context);

            // Seed data
            await SeedRolesAndUsers();
            var vaccines = await SeedVaccinesAndPackages();

            return Ok(ApiResult<object>.Success(new
            {
                Message = "Data seeded successfully.",
                TotalVaccines = vaccines.Count
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

    [HttpPost("seed-policy")]
    public async Task<IActionResult> SeedPolicyData()
    {
        try
        {
            await ClearCancellationPolicyTable(_context);
            //seed policy
            var policies = await SeedPolicies();
            return Ok(ApiResult<object>.Success(new
            {
                Message = "Data seeded successfully.",
                Policy = policies.Count
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

    //Seed other tables
    private async Task SeedRolesAndUsers()
    {
        // Seed Roles
        var roles = new List<Role>
        {
            new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), RoleName = RoleType.Customer },
            new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), RoleName = RoleType.Staff },
            new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), RoleName = RoleType.Admin }
        };

        _logger.Info("Seeding roles...");
        await _context.Roles.AddRangeAsync(roles);
        await _context.SaveChangesAsync();
        _logger.Success("Roles seeded successfully.");

        // Seed Users
        var passwordHasher = new PasswordHasher();
        var users = new List<User>
        {
            new()
            {
                FullName = "Admin Phuc",
                Email = "phucadmin@example.com",
                Gender = true,
                DateOfBirth = new DateTime(1985, 1, 1),
                PhoneNumber = "0393734206",
                PasswordHash = passwordHasher.HashPassword("AdminPassword@"),
                RoleName = RoleType.Admin
            },
            new()
            {
                FullName = "Admin Two",
                Email = "admin2@example.com",
                Gender = false,
                DateOfBirth = new DateTime(1987, 2, 2),
                PhoneNumber = "0987654321",
                PasswordHash = passwordHasher.HashPassword("AdminPassword@"),
                RoleName = RoleType.Admin
            },
            new()
            {
                FullName = "Staff Phúc",
                Email = "staff1@gmail.com",
                Gender = true,
                DateOfBirth = new DateTime(1990, 3, 3),
                PhoneNumber = "1122334455",
                PasswordHash = passwordHasher.HashPassword("1@"),
                RoleName = RoleType.Staff
            },
            new()
            {
                FullName = "Staff uy lê",
                Email = "staff2@gmail.com",
                Gender = false,
                DateOfBirth = new DateTime(1992, 4, 4),
                PhoneNumber = "5566778899",
                PasswordHash = passwordHasher.HashPassword("1@"),
                RoleName = RoleType.Staff,
                ImageUrl =
                    "https://scontent-hkg4-1.xx.fbcdn.net/v/t1.15752-9/475528128_1134900321451127_3323942936519002305_n.png?_nc_cat=108&ccb=1-7&_nc_sid=9f807c&_nc_eui2=AeGc9uwG_xXuF9RJoF7bI17SeurTYb30fQh66tNhvfR9CKznXnCZn4dU5Bc59_JA3_eYgxJX5aKpI0iFLjxy86bK&_nc_ohc=7CpLZ9FbsmgQ7kNvgGSiMCT&_nc_oc=Adi_PRwfWJIV7gbsWdbStchrmVdAHsHWGFV_1nNFo5X716uaWl7yNxMqHEJLoZtyE4_nijOpXbFJrFCXfwWdjZWF&_nc_zt=23&_nc_ht=scontent-hkg4-1.xx&oh=03_Q7cD1gE_4j4t6T7fbVLzoUGvojV-dXqkEzD-KVSHY0aHx9PdgA&oe=67EFCCED"
            }
        };

        _logger.Info("Seeding users...");
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();
        _logger.Success("Users seeded successfully.");
    }


    private async Task<List<Vaccine>> SeedVaccinesAndPackages()
    {
        var vaccines = new List<Vaccine>
        {
            #region VaccineData

            new()
            {
                VaccineName = "BCG",
                Description = "Phòng lao",
                Type = "Vietnam",
                Price = 150000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FBCG.jpg&version_id=null",
                RequiredDoses = 1,
                DoseIntervalDays = 0, // Không cần khoảng cách giữa các mũi
                AvoidChronic = true,
                AvoidAllergy = false,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Pentaxim",
                Description = "Bạch hầu, Ho gà, Uốn ván, Bại liệt, Hib",
                Type = "France",
                Price = 795000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FPentaxim.jpg&version_id=null",
                RequiredDoses = 3,
                DoseIntervalDays = 30, // Mỗi mũi cách nhau 30 ngày
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Infanrix Hexa",
                Description = "6 trong 1 (DTP, Bại liệt, Hib, Viêm gan B)",
                Type = "Belgium",
                Price = 1015000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FInfanrix%20Hexa.jpg&version_id=null",
                RequiredDoses = 3,
                DoseIntervalDays = 28, // Mỗi mũi cách nhau 28 ngày
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Rotateq",
                Description = "Ngừa tiêu chảy do Rotavirus",
                Type = "USA",
                Price = 665000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FRotateq.jpg&version_id=null",
                RequiredDoses = 3,
                DoseIntervalDays = 42, // Mỗi mũi cách nhau 42 ngày
                AvoidChronic = true,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "IPV",
                Description = "Bại liệt (tiêm)",
                Type = "Belgium",
                Price = 450000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FIPV.png&version_id=null",
                RequiredDoses = 4,
                DoseIntervalDays = 60, // Mỗi mũi cách nhau 60 ngày
                AvoidChronic = false,
                AvoidAllergy = false,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },


            new()
            {
                VaccineName = "OPV",
                Description = "Bại liệt (uống)",
                Type = "Vietnam",
                Price = 100000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FOPV.png&version_id=null",
                RequiredDoses = 4,
                DoseIntervalDays = 30, // Mỗi mũi cách nhau 30 ngày
                AvoidChronic = false,
                AvoidAllergy = false,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Measles (MVVac)",
                Description = "Sởi đơn",
                Type = "Vietnam",
                Price = 396000,
                RequiredDoses = 2,
                DoseIntervalDays = 90, // Mỗi mũi cách nhau 90 ngày (3 tháng)
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FOPV.png&version_id=null",
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "MMR II",
                Description = "Sởi - Quai bị - Rubella",
                Type = "USA",
                Price = 445000,
                RequiredDoses = 2,
                DoseIntervalDays = 180, // Cách nhau 6 tháng
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FMMR.jpg&version_id=null",
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Varivax",
                Description = "Thủy đậu",
                Type = "USA",
                Price = 1085000,
                RequiredDoses = 2,
                DoseIntervalDays = 90, // Cách nhau 3 tháng
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FVARIVAX.jpg&version_id=null",
                AvoidChronic = true,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Havrix",
                Description = "Viêm gan A",
                Type = "UK",
                Price = 850000,
                RequiredDoses = 2,
                DoseIntervalDays = 180,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FHavrix.jpg&version_id=null",
                AvoidChronic = false,
                AvoidAllergy = false,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Ixiaro",
                Description = "Viêm não Nhật Bản",
                Type = "Austria",
                Price = 1300000,
                RequiredDoses = 2,
                DoseIntervalDays = 28,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FIxiaro.jpg&version_id=null",
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Typhim Vi",
                Description = "Thương hàn",
                Type = "France",
                Price = 900000,
                RequiredDoses = 1,
                DoseIntervalDays = 0,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FTyphim%20Vi.jpg&version_id=null",
                AvoidChronic = false,
                AvoidAllergy = false,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Verorab",
                Description = "Dại",
                Type = "France",
                Price = 950000,
                RequiredDoses = 4,
                DoseIntervalDays = 7,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FVerorab.jpg&version_id=null",
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Menactra",
                Description = "Viêm màng não mô cầu",
                Type = "USA",
                Price = 1750000,
                RequiredDoses = 1,
                DoseIntervalDays = 0,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FMenactra.jpg&version_id=null",
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Gardasil",
                Description = "HPV (Ngừa ung thư cổ tử cung)",
                Type = "USA",
                Price = 1790000,
                RequiredDoses = 2,
                DoseIntervalDays = 180,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FGardasil.png&version_id=null",
                AvoidChronic = false,
                AvoidAllergy = false,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Vaxigrip Tetra",
                Description = "Cúm mùa",
                Type = "France",
                Price = 356000,
                RequiredDoses = 1,
                DoseIntervalDays = 0,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FVaxigrip.jpg&version_id=null",
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Pfizer-BioNTech COVID-19",
                Description = "COVID-19 (5+)",
                Type = "USA",
                Price = 1200000,
                RequiredDoses = 2,
                DoseIntervalDays = 21,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FPfizer-BioNTech%20COVID-19.png&version_id=null",
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = true
            },
            //DONE
            new()
            {
                VaccineName = "Hexaxim",
                Description = "6 trong 1 (DTP, Bại liệt, Hib, Viêm gan B)",
                Type = "France",
                Price = 950000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FHexaxim.jpg&version_id=null",
                RequiredDoses = 3,
                DoseIntervalDays = 28, // Mỗi mũi cách nhau 28 ngày
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Rotarix",
                Description = "Ngừa tiêu chảy do Rotavirus",
                Type = "Belgium",
                Price = 650000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FRotarix.jpg&version_id=null",
                RequiredDoses = 2,
                DoseIntervalDays = 28, // Mỗi mũi cách nhau 28 ngày
                AvoidChronic = true,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Synflorix",
                Description = "Viêm phổi do phế cầu khuẩn (PCV13)",
                Type = "Belgium",
                Price = 1200000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FSynflorix.jpg&version_id=null",
                RequiredDoses = 3,
                DoseIntervalDays = 60, // Mỗi mũi cách nhau 60 ngày
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Priorix",
                Description = "Sởi, Quai bị, Rubella",
                Type = "Belgium",
                Price = 850000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FPriorix.jpg&version_id=null",
                RequiredDoses = 2,
                DoseIntervalDays = 180, // Mỗi mũi cách nhau 6 tháng
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Imojev",
                Description = "Viêm não Nhật Bản (cải tiến)",
                Type = "France",
                Price = 1450000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FImojev.jpg&version_id=null",
                RequiredDoses = 1,
                DoseIntervalDays = 0, // Không cần khoảng cách giữa các mũi
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Varilrix",
                Description = "Thủy đậu (chủng ngừa)",
                Type = "Switzerland",
                Price = 1100000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FVarilrix.jpg&version_id=null",
                RequiredDoses = 2,
                DoseIntervalDays = 90, // Mỗi mũi cách nhau 3 tháng
                AvoidChronic = true,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Twinrix",
                Description = "Viêm gan A & B",
                Type = "UK",
                Price = 1300000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FTwinrix.jpg&version_id=null",
                RequiredDoses = 3,
                DoseIntervalDays = 180, // Mỗi mũi cách nhau 6 tháng
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },

            new()
            {
                VaccineName = "Tetraxim",
                Description = "DTP (Bạch hầu, Ho gà, Uốn ván) + Bại liệt (tiêm)",
                Type = "France",
                Price = 780000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FTetraxim.jpg&version_id=null",
                RequiredDoses = 3,
                DoseIntervalDays = 30, // Mỗi mũi cách nhau 30 ngày
                AvoidChronic = false,
                AvoidAllergy = false,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Prevenar 13",
                Description = "Vaccine phòng ngừa viêm phổi do phế cầu khuẩn (13 chủng)",
                Type = "USA",
                Price = 1500000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FPrevenar%2013.jpg&version_id=null",
                RequiredDoses = 3,
                DoseIntervalDays = 60, // Mỗi mũi cách nhau 60 ngày
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            },
            new()
            {
                VaccineName = "Morcvax",
                Description = "Vaccine phòng ngừa Mumps, Rubella, Measles (Sởi, Quai bị, Rubella)",
                Type = "USA",
                Price = 850000,
                PicUrl =
                    "https://minio.ae-tao-fullstack-api.site/api/v1/buckets/vaccinacare-bucket/objects/download?preview=true&prefix=vaccines%2FMorcvax.jpg&version_id=null",
                RequiredDoses = 2,
                DoseIntervalDays = 180, // Mỗi mũi cách nhau 6 tháng
                AvoidChronic = false,
                AvoidAllergy = true,
                HasDrugInteraction = false,
                HasSpecialWarning = false
            }

            #endregion
        };

        // Seed the vaccines first
        _logger.Info("Seeding vaccines...");
        await _context.Vaccines.AddRangeAsync(vaccines);
        await _context.SaveChangesAsync();
        _logger.Success("Vaccines seeded successfully.");

        // Chuyển danh sách vaccine thành dictionary để tra cứu nhanh
        var vaccineDictionary = vaccines.ToDictionary(v => v.VaccineName);

        // Khởi tạo các vaccine packages
        var vaccinePackages = new List<VaccinePackage>
        {
            #region Package one

            new()
            {
                PackageName = "Children's package from 0-2 years old",
                Description =
                    "Children need early and scheduled vaccine to have protective antibodies before exposure to dangerous pathogens.",
                VaccinePackageDetails = new List<VaccinePackageDetail>
                {
                    new() { VaccineId = vaccineDictionary["Hexaxim"].Id, DoseOrder = 1 },
                    new() { VaccineId = vaccineDictionary["Rotarix"].Id, DoseOrder = 2 },
                    new() { VaccineId = vaccineDictionary["Synflorix"].Id, DoseOrder = 3 },
                    new() { VaccineId = vaccineDictionary["Vaxigrip Tetra"].Id, DoseOrder = 4 },
                    new() { VaccineId = vaccineDictionary["Priorix"].Id, DoseOrder = 5 },
                    new() { VaccineId = vaccineDictionary["Imojev"].Id, DoseOrder = 6 },
                    new() { VaccineId = vaccineDictionary["Menactra"].Id, DoseOrder = 7 },
                    new() { VaccineId = vaccineDictionary["Varilrix"].Id, DoseOrder = 8 },
                    new() { VaccineId = vaccineDictionary["Twinrix"].Id, DoseOrder = 9 }
                }
            },

            #endregion

            #region Package two

            new()
            {
                PackageName = "Pre-school package from 3-9 years old",
                Description =
                    "The pre -school period is an important transition period for the child's immune system.",
                VaccinePackageDetails = new List<VaccinePackageDetail>
                {
                    new() { VaccineId = vaccineDictionary["Tetraxim"].Id, DoseOrder = 1 }, //chưa có
                    new() { VaccineId = vaccineDictionary["Prevenar 13"].Id, DoseOrder = 2 }, //chưa có
                    new() { VaccineId = vaccineDictionary["Morcvax"].Id, DoseOrder = 9 }, //chưa có
                    new() { VaccineId = vaccineDictionary["Vaxigrip Tetra"].Id, DoseOrder = 3 },
                    new() { VaccineId = vaccineDictionary["Imojev"].Id, DoseOrder = 4 },
                    new() { VaccineId = vaccineDictionary["Menactra"].Id, DoseOrder = 5 },
                    new() { VaccineId = vaccineDictionary["Varivax"].Id, DoseOrder = 6 },
                    new() { VaccineId = vaccineDictionary["MMR II"].Id, DoseOrder = 7 },
                    new() { VaccineId = vaccineDictionary["Typhim Vi"].Id, DoseOrder = 8 },
                    new() { VaccineId = vaccineDictionary["Twinrix"].Id, DoseOrder = 10 }
                }
            }

            #endregion
        };

        #region Calculate Package Price

        foreach (var package in vaccinePackages)
        {
            decimal? totalPrice = 0;

            // Duyệt qua từng VaccinePackageDetail để cộng dồn giá trị
            foreach (var detail in package.VaccinePackageDetails)
            {
                var vaccine = vaccineDictionary.Values.FirstOrDefault(v => v.Id == detail.VaccineId);
                if (vaccine != null) totalPrice += vaccine.Price;
            }

            // Giảm 20% so với totalPrice
            var discountedPrice = totalPrice * 0.8m; // 20% giảm giá
            package.Price = discountedPrice;
            // In ra giá đã giảm
            Console.WriteLine($"Original total price for package '{package.PackageName}' is: {totalPrice}");
            Console.WriteLine($"Discounted price for package '{package.PackageName}' is: {discountedPrice}");
        }

        #endregion

        // Lưu trữ vaccine packages vào cơ sở dữ liệu
        _logger.Info("Seeding vaccine packages...");
        await _context.VaccinePackages.AddRangeAsync(vaccinePackages);
        await _context.SaveChangesAsync();
        _logger.Success("Vaccine packages seeded successfully.");

        return vaccines;
    }

    private async Task ClearDatabase(VaccinaCareDbContext context)
    {
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            _logger.Info("Bắt đầu xóa dữ liệu trong database...");

            var tablesToDelete = new List<Func<Task>>
            {
                () => context.VaccinePackageDetails.ExecuteDeleteAsync(), // Delete dependent VaccinePackageDetail first
                () => context.Invoices.ExecuteDeleteAsync(),
                () => context.Payments.ExecuteDeleteAsync(),
                () => context.Notifications.ExecuteDeleteAsync(),
                () => context.AppointmentsVaccines.ExecuteDeleteAsync(),
                () => context.Appointments.ExecuteDeleteAsync(),
                () => context.CancellationPolicies.ExecuteDeleteAsync(),
                () => context.Children.ExecuteDeleteAsync(),
                () => context.Feedbacks.ExecuteDeleteAsync(),
                () => context.PackageProgresses.ExecuteDeleteAsync(),
                () => context.PaymentTransactions.ExecuteDeleteAsync(),
                () => context.UsersVaccinationServices.ExecuteDeleteAsync(),
                () => context.VaccinationRecords.ExecuteDeleteAsync(),
                () => context.VaccineSuggestions.ExecuteDeleteAsync(),
                () => context.VaccineIntervalRules.ExecuteDeleteAsync(),
                // Delete Vaccine packages before deleting the vaccine itself
                () => context.VaccinePackages.ExecuteDeleteAsync(),
                () => context.Vaccines.ExecuteDeleteAsync(), // Now we can delete the Vaccine
                () => context.Users.ExecuteDeleteAsync(),
                () => context.Roles.ExecuteDeleteAsync()
            };

            foreach (var deleteFunc in tablesToDelete) await deleteFunc();

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

    //
    private async Task<List<CancellationPolicy>> SeedPolicies()
    {
        var policies = new List<CancellationPolicy>
        {
            new()
            {
                PolicyName = "Standard Cancellation Policy",
                Description =
                    "Customers are required to make 100% payment for the appointment at the time of booking. " +
                    "Once the payment has been made, cancellation is no longer possible. " +
                    "However, rescheduling is available, provided that the rescheduling request is made at least 24 hours in advance of the scheduled appointment. " +
                    "We kindly ask for your understanding and cooperation with this policy to ensure smooth operations for all our valued customers.",
                CancellationDeadline =
                    24, 
                PenaltyFee = 0m 
            }
        };

        if (!_context.CancellationPolicies.Any())
        {
            await _context.CancellationPolicies.AddRangeAsync(policies);
            await _context.SaveChangesAsync();
        }

        return policies;
    }

    private async Task ClearCancellationPolicyTable(VaccinaCareDbContext context)
    {
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            _logger.Info("Bắt đầu xóa dữ liệu trong policyTable...");

            var policyTable = new List<Func<Task>>
            {
                () => context.CancellationPolicies.ExecuteDeleteAsync()
            };

            foreach (var deleteFunc in policyTable) await deleteFunc();

            await transaction.CommitAsync();
            _logger.Success("Xóa sạch dữ liệu trong policyTable thành công.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.Error($"Xóa dữ liệu thất bại: {ex.Message}");
            throw;
        }
    }
}