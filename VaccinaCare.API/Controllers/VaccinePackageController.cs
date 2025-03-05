using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Service;
using VaccinaCare.Application.Service.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.ChildDTOs;
using VaccinaCare.Domain.DTOs.VaccinePackageDTOs;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/packages")]
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
        if (dto == null)
        {
            _logger.Warn("CreateVaccinePackage: Vaccine Package data is null");
            return BadRequest(ApiResult<object>.Error("400 - Invalid registration data."));
        }

        try
        {
            var createdPackage = await _vaccinePackageService.CreateVaccinePackageAsync(dto);
            if (createdPackage == null)
                return BadRequest(
                    ApiResult<object>.Error("400 - Vaccine package creation failed. Please check input data."));

            return Ok(ApiResult<VaccinePackageDTO>.Success(createdPackage, "Vaccine package created successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during creation."));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllVaccinePackages()
    {
        try
        {
            var vaccinePackages = await _vaccinePackageService.GetAllVaccinePackagesAsync();
            if (vaccinePackages == null || vaccinePackages.Count == 0)
                return NotFound(ApiResult<object>.Error("404 - No vaccine packages available."));

            return Ok(ApiResult<List<VaccinePackageDTO>>.Success(vaccinePackages,
                "Vaccine packages retrieved successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500,
                ApiResult<object>.Error("An unexpected error occurred while retrieving vaccine packages."));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetVaccinePackageById(Guid id)
    {
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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVaccinePackage(Guid id)
    {
        try
        {
            var isDeleted = await _vaccinePackageService.DeleteVaccinePackageByIdAsync(id);
            if (!isDeleted) return NotFound(ApiResult<object>.Error("404 - Vaccine package not found."));

            return Ok(ApiResult<object>.Success(null, "Vaccine package deleted successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("500 - Internal server error."));
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVaccinePackage(Guid id, [FromBody] UpdateVaccinePackageDTO dto)
    {
        if (dto == null) return BadRequest(ApiResult<object>.Error("400 - Invalid request data."));

        var updatedPackage = await _vaccinePackageService.UpdateVaccinePackageByIdAsync(id, dto);

        if (updatedPackage == null) return NotFound(ApiResult<object>.Error("404 - Vaccine package not found."));

        return Ok(ApiResult<VaccinePackageDTO>.Success(updatedPackage, "Vaccine package updated successfully."));
    }

    [HttpGet("all-types")]
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

    [HttpGet("all-pagings")]
    public async Task<IActionResult> GetAllVaccinesAndPackages(
  [FromQuery] string? searchName,
  [FromQuery] string? searchDescription,
  [FromQuery] int pageNumber = 1,
  [FromQuery] int pageSize = 10)
    {
        _logger.Info("Fetching all vaccines and vaccine packages with filtering and pagination.");
        try
        {
            var result = await _vaccinePackageService.GetAllVaccinesAndPackagesAsyncPaging(searchName, searchDescription, pageNumber, pageSize);
            if (result.Items.Count == 0)
            {
                _logger.Warn("No vaccines or vaccine packages found.");
                return NotFound(ApiResult<object>.Error("404 - No vaccines or vaccine packages available."));
            }

            _logger.Success("Vaccines and vaccine packages retrieved successfully.");
            return Ok(ApiResult<object>.Success(result, "Vaccines and vaccine packages retrieved successfully."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error while fetching vaccines and vaccine packages: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred while retrieving vaccines and vaccine packages."));
        }
    }
}