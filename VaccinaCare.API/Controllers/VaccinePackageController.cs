using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.VaccinePackageDTOs;
namespace VaccinaCare.API.Controllers;
[ApiController]
[Route("api/[controller]")]
public class VaccinePackageController : ControllerBase
{
    private readonly IVaccinePackageService _vaccinePackageService;
    private readonly ILoggerService _logger;

    public VaccinePackageController(IVaccinePackageService vaccinePackageService, ILoggerService logger)
    {
        _vaccinePackageService = vaccinePackageService;
        _logger = logger;
    }
    [HttpPost]
    public async Task<IActionResult> CreateVaccinePackage([FromBody] CreateVaccinePackageDTO dto)
    {
        _logger.Info("Create vaccine package received.");
        if (dto == null)
        {
            _logger.Warn("CreateVaccinePackage: Vaccine Package data is null");
            return BadRequest(ApiResult<object>.Error("400 - Invalid registration data."));
        }

        try
        {
            _logger.Info($"CreateVaccinePackage: Attemping to create a new vaccine package - {dto.PackageName}.");

            var createdPackage = await _vaccinePackageService.CreateVaccinePackageAsync(dto);
            if (createdPackage == null)
            {
                _logger.Warn("CreateVaccinePackage: Vaccine pacakage creation failed due to validation issues");
                return BadRequest(ApiResult<object>.Error("400 - Vaccine package creation failed. Please check input data."));
            }
            _logger.Success($"CreateVaccinePackage: Vaccine Package '{createdPackage.PackageName}' created successfully");
            return Ok(ApiResult<VaccinePackageDTO>.Success(createdPackage, "Vaccine package created successfully."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error during creation: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during creation."));
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAllVaccinePackages()
    {
        _logger.Info("GetAllVaccinePackages: Fetching all vaccine packages.");

        try
        {
            var vaccinePackages = await _vaccinePackageService.GetAllVaccinePackagesAsync();
            if (vaccinePackages == null || vaccinePackages.Count == 0)
            {
                _logger.Warn("GetAllVaccinePackages: No vaccine packages found.");
                return NotFound(ApiResult<object>.Error("404 - No vaccine packages available."));
            }

            _logger.Success($"GetAllVaccinePackages: {vaccinePackages.Count} vaccine packages retrieved successfully.");
            return Ok(ApiResult<List<VaccinePackageDTO>>.Success(vaccinePackages, "Vaccine packages retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error while fetching vaccine packages: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred while retrieving vaccine packages."));
        }
    }
}


