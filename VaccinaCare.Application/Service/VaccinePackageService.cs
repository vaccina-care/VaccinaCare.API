using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.DTOs.VaccinePackageDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service;

public class VaccinePackageService : IVaccinePackageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILoggerService _loggerService;
    private readonly IClaimsService _claimsService;

    public VaccinePackageService(IUnitOfWork unitOfWork, ILoggerService loggerService, IClaimsService claimsService)
    {
        _unitOfWork = unitOfWork;
        _loggerService = loggerService;
        _claimsService = claimsService;
    }

    public async Task<VaccinePackageDTO> CreateVaccinePackageAsync(CreateVaccinePackageDTO dto)
    {
        _loggerService.Info($"Creating Vaccine Package: {dto.PackageName}");

        try
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrWhiteSpace(dto.PackageName))
                validationErrors.Add("Package name is required.");

            if (string.IsNullOrWhiteSpace(dto.Description))
                validationErrors.Add("Package description is required.");

            if (validationErrors.Any())
            {
                _loggerService.Warn(
                    $"Validation failed for CreateVaccinePackageDTO: {string.Join("; ", validationErrors)}");
                throw new ArgumentException(string.Join("; ", validationErrors));
            }

            // Calculate total vaccine price from the details provided
            decimal totalVaccinePrice = 0;
            foreach (var vaccineDetail in dto.VaccineDetails)
            {
                var vaccine = await _unitOfWork.VaccineRepository.GetByIdAsync(vaccineDetail.VaccineId);
                if (vaccine != null) totalVaccinePrice += vaccine.Price ?? 0;
            }

            // Log the old price (before discount)
            _loggerService.Info(
                $"Total vaccine price (before discount) for package '{dto.PackageName}': {totalVaccinePrice:C}");

            // Apply the 10% discount
            var discountedPrice = totalVaccinePrice * 0.9m;

            // Log the new price (after discount)
            _loggerService.Info(
                $"Discounted price (after 10% discount) for package '{dto.PackageName}': {discountedPrice:C}");

            var vaccinePackage = new VaccinePackage
            {
                PackageName = dto.PackageName,
                Description = dto.Description,
                Price = discountedPrice, // Set the discounted price
                VaccinePackageDetails = dto.VaccineDetails.Select(vd => new VaccinePackageDetail
                {
                    VaccineId = vd.VaccineId,
                    DoseOrder = vd.DoseOrder
                }).ToList()
            };

            await _unitOfWork.VaccinePackageRepository.AddAsync(vaccinePackage);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Info($"Created Vaccine Package successfully: {dto.PackageName}");

            var result = new VaccinePackageDTO
            {
                Id = vaccinePackage.Id,
                PackageName = vaccinePackage.PackageName,
                Description = vaccinePackage.Description,
                Price = vaccinePackage.Price,
                VaccineDetails = vaccinePackage.VaccinePackageDetails.Select(vd => new VaccinePackageDetailDTO
                {
                    VaccineId = vd.VaccineId ?? Guid.Empty,
                    DoseOrder = vd.DoseOrder ?? 0
                }).ToList()
            };
            return result;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error creating Vaccine Package: {ex.Message}");
            throw;
        }
    }

    public async Task<List<VaccinePackageDTO>> GetAllVaccinePackagesAsync()
    {
        try
        {
            _loggerService.Info("Fetching all Vaccine Packages...");

            var vaccinePackages = await _unitOfWork.VaccinePackageRepository.GetAllAsync();

            var allPackageDetails = await _unitOfWork.VaccinePackageDetailRepository.GetAllAsync();

            var activeVaccines = await _unitOfWork.VaccineRepository.GetAllAsync(v => !v.IsDeleted);

            var result = vaccinePackages.Select(vp => new VaccinePackageDTO
            {
                Id = vp.Id,
                PackageName = vp.PackageName,
                Description = vp.Description,
                Price = vp.Price,
                VaccineDetails = allPackageDetails
                    .Where(vd => vd.PackageId == vp.Id && activeVaccines.Any(v => v.Id == vd.VaccineId))
                    .Select(vd => new VaccinePackageDetailDTO
                    {
                        VaccineId = vd.VaccineId ?? Guid.Empty,
                        DoseOrder = vd.DoseOrder ?? 0
                    }).ToList()
            }).ToList();

            _loggerService.Info($"Fetched {result.Count} Vaccine Packages successfully.");
            return result;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error fetching Vaccine Packages: {ex.Message}");
            throw;
        }
    }

    public async Task<PagedResult<VaccinePackageResultDTO>> GetAllVaccinesAndPackagesAsync(string? searchName,
        string? searchDescription, int pageNumber, int pageSize)
    {
        try
        {
            _loggerService.Info("Fetching all Vaccines and Vaccine Packages with filtering and pagination...");

            var vaccines = await _unitOfWork.VaccineRepository.GetAllAsync(v => !v.IsDeleted);
            var packages = await _unitOfWork.VaccinePackageRepository.GetAllAsync();
            var packageDetails = await _unitOfWork.VaccinePackageDetailRepository.GetAllAsync();

            // Backup original vaccine IDs for packageDetails
            var allVaccineIds = new HashSet<Guid>(vaccines.Select(v => v.Id));

            // Filtering
            if (!string.IsNullOrWhiteSpace(searchName))
            {
                var searchNameLower = searchName.Trim().ToLower();
                vaccines = vaccines.Where(v => v.VaccineName.ToLower().Contains(searchNameLower)).ToList();
                packages = packages.Where(p => p.PackageName.ToLower().Contains(searchNameLower)).ToList();
            }

            if (!string.IsNullOrWhiteSpace(searchDescription))
            {
                var searchDescriptionLower = searchDescription.Trim().ToLower();
                vaccines = vaccines.Where(v => v.Description.ToLower().Contains(searchDescriptionLower)).ToList();
                packages = packages.Where(p => p.Description.ToLower().Contains(searchDescriptionLower)).ToList();
            }

            // If vaccines are filtered to empty, revert to original vaccine list to preserve packageDetails
            var filteredVaccineIds = vaccines.Any() ? new HashSet<Guid>(vaccines.Select(v => v.Id)) : allVaccineIds;

            // Mapping to DTOs
            var vaccineDTOs = vaccines.Select(vaccine => new VaccineDto
            {
                Id = vaccine.Id,
                VaccineName = vaccine.VaccineName,
                Description = vaccine.Description,
                PicUrl = vaccine.PicUrl,
                Type = vaccine.Type,
                Price = vaccine.Price,
                RequiredDoses = vaccine.RequiredDoses,
                DoseIntervalDays = vaccine.DoseIntervalDays,
                ForBloodType = vaccine.ForBloodType,
                AvoidChronic = vaccine.AvoidChronic,
                AvoidAllergy = vaccine.AvoidAllergy,
                HasDrugInteraction = vaccine.HasDrugInteraction,
                HasSpecialWarning = vaccine.HasSpecialWarning
            }).ToList();

            var packageDTOs = packages.Select(p => new VaccinePackageDTO
            {
                Id = p.Id,
                PackageName = p.PackageName,
                Description = p.Description,
                Price = p.Price,
                VaccineDetails = packageDetails
                    .Where(d => d.PackageId == p.Id && filteredVaccineIds.Contains(d.VaccineId ?? Guid.Empty))
                    .Select(d => new VaccinePackageDetailDTO
                    {
                        VaccineId = d.VaccineId ?? Guid.Empty,
                        DoseOrder = d.DoseOrder ?? 0
                    }).ToList()
            }).ToList();

            // Pagination
            var totalItems = vaccineDTOs.Count + packageDTOs.Count;
            vaccineDTOs = vaccineDTOs.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            packageDTOs = packageDTOs.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            _loggerService.Info(
                $"Fetched {vaccineDTOs.Count} Vaccines and {packageDTOs.Count} Vaccine Packages successfully.");
            return new PagedResult<VaccinePackageResultDTO>(new List<VaccinePackageResultDTO>
            {
                new() { Vaccines = vaccineDTOs, VaccinePackages = packageDTOs }
            }, totalItems, pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error fetching Vaccines and Vaccine Packages: {ex.Message}");
            throw;
        }
    }

    public async Task<VaccinePackageDTO> GetVaccinePackageByIdAsync(Guid packageId)
    {
        try
        {
            _loggerService.Info($"Fetching Vaccine Package with ID: {packageId}");

            var package =
                await _unitOfWork.VaccinePackageRepository.GetByIdAsync(packageId, vp => vp.VaccinePackageDetails);

            if (package == null)
            {
                _loggerService.Warn($"Vaccine Package with ID {packageId} not found");
                return null;
            }

            var activeVaccines = await _unitOfWork.VaccineRepository.GetAllAsync(v => !v.IsDeleted);

            var result = new VaccinePackageDTO
            {
                Id = package.Id,
                PackageName = package.PackageName,
                Description = package.Description,
                Price = package.Price,
                VaccineDetails = package.VaccinePackageDetails
                    .Where(vd => activeVaccines.Any(v => v.Id == vd.VaccineId))
                    .Select(vd => new VaccinePackageDetailDTO
                    {
                        VaccineId = vd.VaccineId ?? Guid.Empty,
                        DoseOrder = vd.DoseOrder ?? 0
                    }).ToList()
            };

            _loggerService.Info($"Fetched Vaccine Package successfully: {package.PackageName}");
            return result;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error fetching Vaccine Package by ID {ex.Message}");
            throw;
        }
    }

    public async Task<VaccinePackageDTO> UpdateVaccinePackageByIdAsync(Guid packageId, UpdateVaccinePackageDTO dto)
    {
        try
        {
            _loggerService.Info($"Updating Vaccine Package with ID: {packageId}");

            var vaccinePackage =
                await _unitOfWork.VaccinePackageRepository.GetByIdAsync(packageId, vp => vp.VaccinePackageDetails);
            if (vaccinePackage == null)
            {
                _loggerService.Warn($"Vaccine Package with ID {packageId} not found.");
                return null;
            }

            // Cập nhật thông tin cơ bản của package
            if (!string.IsNullOrWhiteSpace(dto.PackageName) && dto.PackageName != vaccinePackage.PackageName)
                vaccinePackage.PackageName = dto.PackageName;

            if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description != vaccinePackage.Description)
                vaccinePackage.Description = dto.Description;

            if (dto.Price.HasValue && dto.Price.Value != vaccinePackage.Price)
                vaccinePackage.Price = dto.Price.Value;

            var existingVaccineDetails = vaccinePackage.VaccinePackageDetails.ToList();
            var newVaccineIds = dto.VaccineDetails?.Select(v => v.VaccineId).ToList() ?? new List<Guid>();

            //Loại bỏ vaccine khỏi package nếu không có trong danh sách mới
            var vaccinesToRemove = existingVaccineDetails
                .Where(v => !newVaccineIds.Contains(v.VaccineId.GetValueOrDefault()))
                .ToList();

            foreach (var vaccineToRemove in vaccinesToRemove)
            {
                _loggerService.Info($"Removing Vaccine {vaccineToRemove.VaccineId} from Package {packageId}");
                await _unitOfWork.VaccinePackageDetailRepository.HardRemove(v => v.Id == vaccineToRemove.Id);
            }

            // Thêm hoặc cập nhật vaccine
            if (dto.VaccineDetails != null && dto.VaccineDetails.Any())
                foreach (var newVaccine in dto.VaccineDetails)
                {
                    var existingVaccine =
                        existingVaccineDetails.FirstOrDefault(v => v.VaccineId == newVaccine.VaccineId);

                    if (existingVaccine != null)
                    {
                        //Cập nhật thứ tự liều nếu có thay đổi
                        if (existingVaccine.DoseOrder != newVaccine.DoseOrder)
                        {
                            _loggerService.Info(
                                $"Updating DoseOrder for Vaccine {newVaccine.VaccineId} to {newVaccine.DoseOrder}");
                            existingVaccine.DoseOrder = newVaccine.DoseOrder;
                        }
                    }
                    else
                    {
                        //Thêm vaccine vào package
                        _loggerService.Info($"Adding new Vaccine {newVaccine.VaccineId} to Package {packageId}");
                        vaccinePackage.VaccinePackageDetails.Add(new VaccinePackageDetail
                        {
                            PackageId = packageId,
                            VaccineId = newVaccine.VaccineId,
                            DoseOrder = newVaccine.DoseOrder
                        });
                    }
                }

            await _unitOfWork.VaccinePackageRepository.Update(vaccinePackage);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Success($"Vaccine Package {packageId} updated successfully.");

            return new VaccinePackageDTO
            {
                Id = vaccinePackage.Id,
                PackageName = vaccinePackage.PackageName,
                Description = vaccinePackage.Description,
                Price = vaccinePackage.Price,
                VaccineDetails = vaccinePackage.VaccinePackageDetails
                    .Select(vd => new VaccinePackageDetailDTO
                    {
                        VaccineId = vd.VaccineId ?? Guid.Empty,
                        DoseOrder = vd.DoseOrder ?? 0
                    }).ToList()
            };
        }
        catch (Exception ex)
        {
            _loggerService.Error($"{ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteVaccinePackageByIdAsync(Guid packageId)
    {
        _loggerService.Info($"Attempting to delete Vaccine Package with ID: {packageId}");

        try
        {
            var vaccinePackage = await _unitOfWork.VaccinePackageRepository.GetByIdAsync(
                packageId,
                vp => vp.VaccinePackageDetails
            );

            if (vaccinePackage == null)
            {
                _loggerService.Warn($"Vaccine Package with ID {packageId} not found.");
                return false;
            }

            if (vaccinePackage.VaccinePackageDetails.Any())
                _unitOfWork.VaccinePackageDetailRepository.SoftRemoveRange(
                    vaccinePackage.VaccinePackageDetails.ToList());

            _unitOfWork.VaccinePackageRepository.SoftRemove(vaccinePackage);
            await _unitOfWork.SaveChangesAsync();

            _loggerService.Info($"Successfully deleted Vaccine Package with ID: {packageId}");
            return true;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error deleting Vaccine Package with ID {packageId}: {ex.Message}");
            return false;
        }
    }
    public async Task<VaccinePackage?> GetMostBookedPackageAsync()
    {
        try
        {
            _loggerService.Info("Fetching most booked vaccine package...");

            var result = await _unitOfWork.VaccinePackageRepository.GetQueryable()
                .Join(_unitOfWork.VaccinePackageDetailRepository.GetQueryable(),
                      vp => vp.Id,
                      vpd => vpd.PackageId,
                      (vp, vpd) => new { vp, vpd })
                .Join(_unitOfWork.AppointmentsVaccineRepository.GetQueryable(),
                      temp => temp.vpd.VaccineId,
                      av => av.VaccineId,
                      (temp, av) => new { temp.vp, av })
                .GroupBy(x => x.vp)
                .Select(g => new
                {
                    Package = g.Key,
                    BookingCount = g.Count()
                })
                .OrderByDescending(x => x.BookingCount)
                .FirstOrDefaultAsync();

            if (result?.Package != null)
            {
                _loggerService.Info($"Most booked package: {result.Package.PackageName}, Count: {result.BookingCount}");
                return result.Package;
            }

            _loggerService.Info("No bookings found for any package.");
            return null;
        }
        catch (Exception ex)
        {
            _loggerService.Error($"Error occurred while getting most booked package: {ex.Message}");
            return null;
        }
    }
}