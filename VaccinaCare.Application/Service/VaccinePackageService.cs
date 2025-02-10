using Microsoft.VisualBasic;
using MimeKit.Cryptography;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.DTOs.VaccinePackageDTOs;
using VaccinaCare.Domain.Entities;
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
            try
            {
                _loggerService.Info($"Creating Vaccine Package: {dto.PackageName}");

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

                var allVaccines = await _unitOfWork.VaccineRepository.GetAllAsync();

                var result = vaccinePackages.Select(vp => new VaccinePackageDTO
                {
                    PackageName = vp.PackageName,
                    Description = vp.Description,
                    Price = vp.Price,
                    VaccineDetails = allPackageDetails
                    .Where(vd => vd.PackageId == vp.Id)
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

        public Task<VaccinePackageDTO> GetVaccinePackageByIdAsync(Guid packageId)
        {
            throw new NotImplementedException();
        }
    }
}
