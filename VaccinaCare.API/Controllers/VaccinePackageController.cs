using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Service.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.ChildDTOs;
using VaccinaCare.Domain.DTOs.VaccinePackageDTOs;
using VaccinaCare.Repository.Commons;

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
                return BadRequest(
                    ApiResult<object>.Error("400 - Vaccine package creation failed. Please check input data."));
            }

            _logger.Success(
                $"CreateVaccinePackage: Vaccine Package '{createdPackage.PackageName}' created successfully");
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
            return Ok(ApiResult<List<VaccinePackageDTO>>.Success(vaccinePackages,
                "Vaccine packages retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error while fetching vaccine packages: {ex.Message}");
            return StatusCode(500,
                ApiResult<object>.Error("An unexpected error occurred while retrieving vaccine packages."));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetVaccinePackageById(Guid id)
    {
        _logger.Info($"Fetching Vaccine Package with ID: {id}");
        try
        {
            if (id == Guid.Empty)
                return BadRequest("Invalid package ID.");

            var result = await _vaccinePackageService.GetVaccinePackageByIdAsync(id);

            if (result == null)
            {
                _logger.Warn($"Vaccine Package with ID {id} not found.");
                return NotFound(ApiResult<object>.Error("404 -  No vaccine package available."));
            }

            _logger.Success($"Get Vaccine Package with ID : {id} retrieved succesfully.");
            return Ok(ApiResult<VaccinePackageDTO>.Success(result, "Vaccine package retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error while fetching vaccine packages: {ex.Message}");
            return StatusCode(500,
                ApiResult<object>.Error("An unexpected error occurred while retrieving vaccine packages."));
        }
    }

    [HttpDelete("id")]
    public async Task<IActionResult> DeleteVaccinePackage(Guid id)
    {
        _logger.Info($"Request received to delete Vaccine Package with ID: {id}");
        try
        {
            var isDeleted = await _vaccinePackageService.DeleteVaccinePackageByIdAsync(id);
            if (!isDeleted)
            {
                _logger.Warn($"Vaccine Package with ID {id} not found.");
                return NotFound(ApiResult<object>.Error("404 - Vaccine package not found."));
            }

            _logger.Success($"Vaccine Package with ID {id} deleted successfully.");
            return Ok(ApiResult<object>.Success(null, "Vaccine package deleted successfully."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Error deleting Vaccine Package with ID {id}: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("500 - Internal server error."));
        }
    }

    [HttpPut("id")]
    public async Task<IActionResult> UpdateVaccinePackage(Guid id, [FromBody] UpdateVaccinePackageDTO dto)
    {
        if (dto == null) return BadRequest(ApiResult<object>.Error("400 - Invalid request data."));

        var updatedPackage = await _vaccinePackageService.UpdateVaccinePackageByIdAsync(id, dto);

        if (updatedPackage == null) return NotFound(ApiResult<object>.Error("404 - Vaccine package not found."));

        return Ok(ApiResult<VaccinePackageDTO>.Success(updatedPackage, "Vaccine package updated successfully."));
    }

    [HttpGet("paging")]
    [ProducesResponseType(typeof(ApiResult<Pagination<VaccinePackageDTO>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetVaccinePackagePaging([FromQuery] PaginationParameter pagination)
    {
        try
        {
            _logger.Info("Received request to get vaccine package list.");

            var vaccinePackages = await _vaccinePackageService.GetVaccinePackagesPaging(pagination);

            _logger.Success($"Fetched {vaccinePackages.Count} vaccine packages successfully.");

            return Ok(new ApiResult<Pagination<VaccinePackageDTO>>
            {
                IsSuccess = true,
                Message = "Vaccine package list retrieved successfully.",
                Data = vaccinePackages
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error while fetching vaccine packages: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving the vaccine package list. Please try again later."
            });
        }
    }
}