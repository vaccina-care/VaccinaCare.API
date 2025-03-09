using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.DTOs.VaccineDTOs.VaccineRecord;

namespace VaccinaCare.API.Controllers;

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
    [Authorize(Policy = "StaffOrCustomerPolicy")]
    [ProducesResponseType(typeof(ApiResult<VaccineRecordDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> AddVaccinationRecord([FromBody] AddVaccineRecordDto addVaccineRecordDto)
    {
        try
        {
            var result = await _vaccineRecordService.AddVaccinationRecordAsync(addVaccineRecordDto);

            var apiResult = new ApiResult<VaccineRecordDto>
            {
                Data = result,
                Message = "Vaccination record added successfully",
                IsSuccess = true
            };
            return Ok(apiResult);
        }
        catch (Exception ex)
        {
            var apiResult = new ApiResult<object>
            {
                Data = null,
                Message = ex.Message,
                IsSuccess = false
            };

            if (ex is ArgumentException || ex is InvalidOperationException) return BadRequest(apiResult);

            return StatusCode(500, apiResult);
        }
    }

    // Get a single vaccination record by ChildId
    [HttpGet("{id}")]
    [Authorize(Policy = "StaffOrCustomerPolicy")]
    [ProducesResponseType(typeof(ApiResult<VaccineRecordDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetVaccinationRecordByChildId(Guid id)
    {
        try
        {
            var result = await _vaccineRecordService.GetVaccinationRecordByRecordIdAsync(id);
            return Ok(new ApiResult<VaccineRecordDto>
            {
                Data = result,
                Message = "Vaccination record fetched successfully",
                IsSuccess = true
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResult<object>
            {
                Data = null,
                Message = ex.Message,
                IsSuccess = false
            });
        }
    }

    // Get list of vaccination records by ChildId
    [HttpGet("list/{childId}")]
    [Authorize(Policy = "StaffOrCustomerPolicy")]
    [ProducesResponseType(typeof(ApiResult<List<VaccineRecordDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetListVaccinationRecordByChildId(Guid childId)
    {
        try
        {
            var result = await _vaccineRecordService.GetListVaccinationRecordByChildIdAsync(childId);
            return Ok(new ApiResult<List<VaccineRecordDto>>
            {
                Data = result,
                Message = "Vaccination records fetched successfully",
                IsSuccess = true
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResult<object>
            {
                Data = null,
                Message = ex.Message,
                IsSuccess = false
            });
        }
    }
}