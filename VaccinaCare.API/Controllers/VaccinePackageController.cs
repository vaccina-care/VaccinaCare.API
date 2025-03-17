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
        if (dto == null) return Ok(ApiResult<object>.Error("400 - Invalid registration data."));

        try
        {
            var createdPackage = await _vaccinePackageService.CreateVaccinePackageAsync(dto);
            if (createdPackage == null)
                return Ok(ApiResult<object>.Error("400 - Vaccine package creation failed. Please check input data."));

            return Ok(ApiResult<VaccinePackageDTO>.Success(createdPackage, "Vaccine package created successfully."));
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<object>.Error("An unexpected error occurred during creation."));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllVaccinePackages()
    {
        try
        {
            var vaccinePackages = await _vaccinePackageService.GetAllVaccinePackagesAsync();
            if (vaccinePackages == null || vaccinePackages.Count == 0)
                return Ok(ApiResult<object>.Error("404 - No vaccine packages available."));

            return Ok(ApiResult<List<VaccinePackageDTO>>.Success(vaccinePackages,
                "Vaccine packages retrieved successfully."));
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<object>.Error("An unexpected error occurred while retrieving vaccine packages."));
        }
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetVaccinePackageById(Guid id)
    {
        try
        {
            if (id == Guid.Empty)
                return Ok(ApiResult<object>.Error("400 - Invalid package ID."));

            var result = await _vaccinePackageService.GetVaccinePackageByIdAsync(id);

            if (result == null) return Ok(ApiResult<object>.Error("404 - No vaccine package available."));

            return Ok(ApiResult<VaccinePackageDTO>.Success(result, "Vaccine package retrieved successfully."));
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<object>.Error("An unexpected error occurred while retrieving vaccine packages."));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVaccinePackage(Guid id)
    {
        try
        {
            var isDeleted = await _vaccinePackageService.DeleteVaccinePackageByIdAsync(id);
            if (!isDeleted)
                return Ok(ApiResult<object>.Error("404 - Vaccine package not found."));

            return Ok(ApiResult<object>.Success(null, "Vaccine package deleted successfully."));
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<object>.Error("500 - Internal server error."));
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVaccinePackage(Guid id, [FromBody] UpdateVaccinePackageDTO dto)
    {
        if (dto == null)
            return Ok(ApiResult<object>.Error("400 - Invalid request data."));

        var updatedPackage = await _vaccinePackageService.UpdateVaccinePackageByIdAsync(id, dto);

        if (updatedPackage == null)
            return Ok(ApiResult<object>.Error("404 - Vaccine package not found."));

        return Ok(ApiResult<VaccinePackageDTO>.Success(updatedPackage, "Vaccine package updated successfully."));
    }

    [HttpGet("all-types")]
    public async Task<IActionResult> GetAllVaccinesAndPackages(
        [FromQuery] string? searchName,
        [FromQuery] string? searchDescription,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var result =
                await _vaccinePackageService.GetAllVaccinesAndPackagesAsync(searchName, searchDescription, pageNumber,
                    pageSize);
            if (result.Items.Count == 0)
                return Ok(ApiResult<object>.Error("404 - No vaccines or vaccine packages available."));

            return Ok(ApiResult<object>.Success(result, "Vaccines and vaccine packages retrieved successfully."));
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<object>.Error(
                "An unexpected error occurred while retrieving vaccines and vaccine packages."));
        }
    }
}