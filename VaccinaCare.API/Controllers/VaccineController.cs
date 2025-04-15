using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.VaccineDTOs;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/vaccines")]
public class VaccineController : ControllerBase
{
    private readonly ILoggerService _logger;
    private readonly IVaccineService _vaccineService;
    private readonly IVaccineSuggestionService _vaccineSuggestionService;

    public VaccineController(IVaccineService vaccineService, ILoggerService logger,
        IVaccineSuggestionService vaccineSuggestionService)
    {
        _vaccineService = vaccineService;
        _logger = logger;
        _vaccineSuggestionService = vaccineSuggestionService;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] string? sortBy,
        [FromQuery] bool isDescending = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            if (page < 1 || pageSize < 1)
                return BadRequest(ApiResult<object>.Error("400 - Invalid pagination parameters."));

            var result = await _vaccineService.GetVaccines(search, type, sortBy, isDescending, page, pageSize);

            if (result == null || !result.Items.Any())
                return NotFound(ApiResult<object>.Error("404 - No vaccines found."));

            return Ok(ApiResult<object>.Success(new
            {
                totalCount = result.TotalCount,
                vaccines = result.Items.Select(v => new
                {
                    v.Id,
                    v.VaccineName,
                    v.Description,
                    v.PicUrl,
                    v.Type,
                    v.Price,
                    v.RequiredDoses
                })
            }, "Vaccine retrieval successful."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during vaccine retrieval."));
        }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResult<VaccineDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetVaccineById([FromRoute] Guid id)
    {
        try
        {
            var vaccine = await _vaccineService.GetVaccineById(id);
            if (vaccine == null) return NotFound(ApiResult<object>.Error("404 - Vaccine not found."));

            return Ok(ApiResult<VaccineDto>.Success(vaccine, "Get vaccine details successfully"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during vaccine retrieval."));
        }
    }

    [Authorize(Policy = "StaffPolicy")]
    [HttpPost]
    [ProducesResponseType(typeof(ApiResult<VaccineDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> Create([FromForm] CreateVaccineDto request, IFormFile vaccinePictureFile)
    {
        if (request == null) return BadRequest(ApiResult<object>.Error("400 - Invalid registration data."));

        if (vaccinePictureFile == null || vaccinePictureFile.Length == 0)
            return BadRequest(ApiResult<object>.Error("400 - Vaccine picture is required."));

        try
        {
            var createdVaccine = await _vaccineService.CreateVaccine(request, vaccinePictureFile);

            if (createdVaccine == null)
                return BadRequest(ApiResult<object>.Error("400 - Vaccine creation failed. Please check input data."));

            return Ok(ApiResult<VaccineDto>.Success(createdVaccine, "Vaccine created successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during creation."));
        }
    }

    [HttpPut("{vaccineId}")]
    [Authorize(Policy = "StaffPolicy")]
    [ProducesResponseType(typeof(ApiResult<VaccineDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> Update(Guid vaccineId, [FromForm] UpdateVaccineDto request,
        IFormFile vaccinePictureFile)
    {
        if (request == null)
            return BadRequest(ApiResult<object>.Error("400 - Vaccine data cannot be null."));

        try
        {
            var updatedVaccine =
                await _vaccineService.UpdateVaccine(vaccineId, request, vaccinePictureFile);
            return Ok(ApiResult<VaccineDto>.Success(updatedVaccine, "Vaccine updated successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during update."));
        }
    }

    [Authorize(Policy = "StaffPolicy")]
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deletedVaccine = await _vaccineService.DeleteVaccine(id);

            if (deletedVaccine == null)
                return BadRequest(ApiResult<object>.Error("400 - Vaccine deleting failed. Please check input data."));

            return Ok(ApiResult<VaccineDto>.Success(deletedVaccine, "Vaccine deleted successfully."));
        }
        catch (ValidationException ex)
        {
            return BadRequest(ApiResult<object>.Error($"400 - Validation error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during deletion."));
        }
    }
}