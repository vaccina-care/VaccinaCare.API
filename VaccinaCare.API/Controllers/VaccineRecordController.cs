using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.VaccineRecordDTOs;

namespace VaccinaCare.API.Controllers;

//DONE CLEAN RETURN SCENARIOS
[ApiController]
[Route("api/vaccination/records")]
public class VaccineRecordController : ControllerBase
{
    private readonly IVaccineRecordService _vaccineRecordService;

    public VaccineRecordController(IVaccineRecordService vaccineRecordService)
    {
        _vaccineRecordService = vaccineRecordService;
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<VaccineRecordDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> AddVaccinationRecord([FromBody] AddVaccineRecordDto addVaccineRecordDto)
    {
        try
        {
            var result = await _vaccineRecordService.AddVaccinationRecordAsync(addVaccineRecordDto);
            return Ok(new ApiResult<VaccineRecordDto>
                { Data = result, Message = "Vaccination record added successfully", IsSuccess = true });
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<object>.Error("An unexpected error occurred during creation."));
        }
    }

    [HttpGet("details/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<VaccineRecordDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetVaccinationRecordByChildId(Guid id)
    {
        try
        {
            var result = await _vaccineRecordService.GetRecordDetailsByIdAsync(id);
            return Ok(new ApiResult<VaccineRecordDto>
            {
                Data = result,
                Message = "Vaccination record fetched successfully",
                IsSuccess = true
            });
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<object>.Error("An unexpected error occurred while fetching the record."));
        }
    }

    // Get list of vaccination records by ChildId
    [HttpGet("{childId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<List<VaccineRecordDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetListVaccinationRecordByChildId(Guid childId)
    {
        try
        {
            var result = await _vaccineRecordService.GetListRecordsByChildIdAsync(childId);
            return Ok(new ApiResult<List<VaccineRecordDto>>
            {
                Data = result,
                Message = "Vaccination records fetched successfully",
                IsSuccess = true
            });
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<object>.Error("An unexpected error occurred while fetching the records."));
        }
    }
}