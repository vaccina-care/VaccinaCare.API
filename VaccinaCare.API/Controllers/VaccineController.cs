using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.VaccineDTOs;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VaccineController : ControllerBase
{
    private readonly IVaccineService _vaccineService;
    private readonly IVaccineSuggestionService _vaccineSuggestionService;
    private readonly ILoggerService _logger;

    public VaccineController(IVaccineService vaccineService, ILoggerService logger,
        IVaccineSuggestionService vaccineSuggestionService)
    {
        _vaccineService = vaccineService;
        _logger = logger;
        _vaccineSuggestionService = vaccineSuggestionService;
    }


    [HttpPost("save-vaccine-suggestions")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> SaveVaccineSuggestions([FromBody] SaveVaccineSuggestionDto request)
    {
        try
        {
            _logger.Info($"Received request to save vaccine suggestions for child ID: {request.ChildId}");

            if (request == null || request.ChildId == Guid.Empty || request.VaccineIds == null ||
                !request.VaccineIds.Any())
            {
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid request. Please provide a valid child ID and a list of vaccine IDs."
                });
            }

            var result = await _vaccineSuggestionService.SaveVaccineSuggestionAsync(request.ChildId, request.VaccineIds);

            if (!result)
            {
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Failed to save vaccine suggestions."
                });
            }

            return Ok(new ApiResult<object>
            {
                IsSuccess = true,
                Message = "Vaccine suggestions saved successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error while saving vaccine suggestions: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while saving the vaccine suggestions. Please try again later."
            });
        }
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
        _logger.Info(
            $"Received request to get vaccines with search: {search}, type: {type}, sortBy: {sortBy}, isDescending: {isDescending}, page: {page}, pageSize: {pageSize}");

        try
        {
            if (page < 1 || pageSize < 1)
            {
                _logger.Warn("Invalid page or pageSize parameters. Both must be greater than 0.");
                return BadRequest(ApiResult<object>.Error("400 - Invalid pagination parameters."));
            }

            var result = await _vaccineService.GetVaccines(search, type, sortBy, isDescending, page, pageSize);

            if (result == null || !result.Items.Any())
            {
                _logger.Warn("No vaccines found with the specified filters.");
                return NotFound(ApiResult<object>.Error("404 - No vaccines found."));
            }

            _logger.Info($"Successfully retrieved {result.Items.Count()} vaccines.");

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
            _logger.Error($"Unexpected error while retrieving vaccines. Error: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during vaccine retrieval."));
        }
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetVaccineById([FromRoute] Guid id)
    {
        _logger.Info($"Received request to get vaccine with ID: {id}");

        try
        {
            var vaccine = await _vaccineService.GetVaccineById(id);
            if (vaccine == null)
            {
                _logger.Warn($"Vaccine with ID {id} not found.");
                return NotFound(ApiResult<object>.Error("404 - Vaccine not found."));
            }

            _logger.Info($"Successfully retrieved vaccine with ID: {id}");

            return Ok(ApiResult<object>.Success(new
            {
                vaccine.Id,
                vaccine.VaccineName,
                vaccine.Description,
                vaccine.PicUrl,
                vaccine.Type,
                vaccine.Price,
                vaccine.RequiredDoses
            }, "Vaccine retrieval successful."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error while retrieving vaccine with ID {id}. Error: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during vaccine retrieval."));
        }
    }


    [Authorize(Policy = "StaffPolicy")]
    [HttpPost("create")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> Create([FromForm] CreateVaccineDTO vaccineDTO)
    {
        _logger.Info("Create vaccine request received.");

        if (vaccineDTO == null)
        {
            _logger.Warn("CreateVaccine: Vaccine data is null.");
            return BadRequest(ApiResult<object>.Error("400 - Invalid registration data."));
        }

        try
        {
            _logger.Info($"CreateVaccine: Attempting to create a new vaccine - {vaccineDTO.VaccineName}.");

            var createdVaccine = await _vaccineService.CreateVaccine(vaccineDTO);

            if (createdVaccine == null)
            {
                _logger.Warn("CreateVaccine: Vaccine creation failed due to validation issues.");
                return BadRequest(ApiResult<object>.Error("400 - Vaccine creation failed. Please check input data."));
            }

            _logger.Success($"CreateVaccine: Vaccine '{createdVaccine.VaccineName}' created successfully.");
            return Ok(ApiResult<CreateVaccineDTO>.Success(createdVaccine, "Vaccine created successfully."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error during creation: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during creation."));
        }
    }


    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> Update(Guid id, [FromBody] VaccineDTO vaccineDTO)
    {
        _logger.Info($"Updated vaccine with ID {id} request received");

        if (vaccineDTO == null)
        {
            _logger.Warn("VaccineDTO is null.");
            return BadRequest(ApiResult<object>.Error("400 - Vaccine data cannot be null."));
        }

        try
        {
            var updateVaccine = await _vaccineService.UpdateVaccine(id, vaccineDTO);
            return Ok(ApiResult<VaccineDTO>.Success(updateVaccine, "Vaccine updated successfully."));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.Warn(ex.Message);
            return NotFound(ApiResult<object>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error during update: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during update."));
        }
    }


    [Authorize(Policy = "StaffPolicy")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> Delete(Guid id)
    {
        _logger.Info($"Delete vaccine with ID {id} request received.");

        try
        {
            var deletedVaccine = await _vaccineService.DeleteVaccine(id);

            if (deletedVaccine == null)
            {
                _logger.Warn($"DeleteVaccine: Failed to delete vaccine with ID {id} due to validation issues.");
                return BadRequest(ApiResult<object>.Error("400 - Vaccine deleting failed. Please check input data."));
            }

            _logger.Success($"DeleteVaccine: Vaccine with name '{deletedVaccine.VaccineName}' deleted successfully.");
            return Ok(ApiResult<VaccineDTO>.Success(deletedVaccine, "Vaccine deleted successfully."));
        }
        catch (ValidationException ex)
        {
            _logger.Warn($"DeleteVaccine: Validation error while deleting vaccine with ID {id}. Error: {ex.Message}");
            return BadRequest(ApiResult<object>.Error($"400 - Validation error: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error during deletion: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during deletion."));
        }
    }
}