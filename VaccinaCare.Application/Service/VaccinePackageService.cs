using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.VisualBasic;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.VaccinePackageDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Commons;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.Application.Service
{
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

                if (dto.Price <= 0)
                    validationErrors.Add("Price cannot be negative or zero.");

                // Cần xem xét lại vì có thể tạo Packgae trước rồi thêm Vaccine vào sau
                //if (dto.VaccineDetails == null || !dto.VaccineDetails.Any())
                //    validationErrors.Add("At least one vaccine detail is required.");

                if (validationErrors.Any())
                {
                    _loggerService.Warn($"Validation failed for CreateVaccinePackageDTO: {string.Join("; ", validationErrors)}");
                    throw new ArgumentException(string.Join("; ", validationErrors));
                }

                var vaccinePackage = new VaccinePackage
                {
                    PackageName = dto.PackageName,
                    Description = dto.Description,
                    Price = dto.Price,
                    VaccinePackageDetails = dto.VaccineDetails.Select(vd => new VaccinePackageDetail
                    {
                        VaccineId = vd.VaccineId,
                        DoseOrder = vd.DoseOrder,
                    }).ToList()
                };
                await _unitOfWork.VaccinePackageRepository.AddAsync(vaccinePackage);
                await _unitOfWork.SaveChangesAsync();

                var vaccineIds = vaccinePackage.VaccinePackageDetails
                    .Select(vd => vd.VaccineId)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

                var allVaccines = await _unitOfWork.VaccineRepository.GetAllAsync(v => vaccineIds.Contains(v.Id));

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
                {
                    _unitOfWork.VaccinePackageDetailRepository.SoftRemoveRange(vaccinePackage.VaccinePackageDetails.ToList());
                }

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

        public async Task<VaccinePackageDTO> GetVaccinePackageByIdAsync(Guid packageId)
        {
            try
            {
                _loggerService.Info($"Fetching Vaccine Package with ID: {packageId}");

                var package = await _unitOfWork.VaccinePackageRepository.GetByIdAsync(packageId, vp => vp.VaccinePackageDetails);

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

        public async Task<Pagination<VaccinePackageDTO>> GetVaccinePackagesPaging(PaginationParameter pagination)
        {
            try
            {
                _loggerService.Info($"Fetching vaccine package with pagination: Page {pagination.PageIndex}, Size {pagination.PageSize}");

                var query = _unitOfWork.VaccinePackageRepository.GetQueryable()
                    .Include(vp => vp.VaccinePackageDetails); 

                int totalPackages = await query.CountAsync();

                var packages = await query
                    .OrderBy(vp => vp.PackageName)
                    .Skip((pagination.PageIndex - 1) * pagination.PageSize)
                    .Take(pagination.PageSize)
                    .ToListAsync();

                if (!packages.Any())
                {
                    _loggerService.Warn($"No vaccine packages found on page {pagination.PageIndex}.");
                    return new Pagination<VaccinePackageDTO>(new List<VaccinePackageDTO>(), 0, pagination.PageIndex, pagination.PageSize);
                }

                _loggerService.Success($"Retrieved {packages.Count} vaccine packages on page {pagination.PageIndex}");

                var packageDtos = packages.Select(package => new VaccinePackageDTO
                {
                    Id = package.Id,
                    PackageName = package.PackageName,
                    Description = package.Description,
                    Price = package.Price,
                    VaccineDetails = package.VaccinePackageDetails
                        .Select(vd => new VaccinePackageDetailDTO
                        {
                            VaccineId = vd.VaccineId ?? Guid.Empty,
                            DoseOrder = vd.DoseOrder ?? 0
                        }).ToList()
                }).ToList();

                return new Pagination<VaccinePackageDTO>(packageDtos, totalPackages, pagination.PageIndex, pagination.PageSize);
            }
            catch (Exception ex)
            {
                _loggerService.Error($"Error while fetching vaccine package: {ex.Message}");
                throw new Exception("An error occurred while fetching vaccine package. Please try again later");
            }
        }
        
        public async Task<VaccinePackageDTO> UpdateVaccinePackageByIdAsync(Guid packageId, UpdateVaccinePackageDTO dto)
        {
            _loggerService.Info($"Updating Vaccine Package with ID: {packageId}");


            var vaccinePackage = await _unitOfWork.VaccinePackageRepository.GetByIdAsync(packageId, vp => vp.VaccinePackageDetails);
            if (vaccinePackage == null)
            {
                _loggerService.Warn($"Vaccine Package with ID {packageId} not found.");
                return null;
            }


            if (!string.IsNullOrWhiteSpace(dto.PackageName) && dto.PackageName != vaccinePackage.PackageName)
                vaccinePackage.PackageName = dto.PackageName;

            if (!string.IsNullOrWhiteSpace(dto.Description) && dto.Description != vaccinePackage.Description)
                vaccinePackage.Description = dto.Description;

            if (dto.Price.HasValue && dto.Price.Value != vaccinePackage.Price)
                vaccinePackage.Price = dto.Price.Value;


            var existingVaccineDetails = vaccinePackage.VaccinePackageDetails.ToList();

            if (dto.VaccineDetails != null && dto.VaccineDetails.Any())
            {
                foreach (var newVaccine in dto.VaccineDetails)
                {
                    var existingVaccine = existingVaccineDetails.FirstOrDefault(v => v.VaccineId == newVaccine.VaccineId);

                    if (existingVaccine != null)
                    {

                        if (existingVaccine.DoseOrder != newVaccine.DoseOrder)
                        {
                            existingVaccine.DoseOrder = newVaccine.DoseOrder;
                        }
                    }
                    else
                    {

                        vaccinePackage.VaccinePackageDetails.Add(new VaccinePackageDetail
                        {
                            PackageId = packageId,
                            VaccineId = newVaccine.VaccineId,
                            DoseOrder = newVaccine.DoseOrder
                        });
                    }
                }
            }

            _unitOfWork.VaccinePackageRepository.Update(vaccinePackage);
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
    }
}
