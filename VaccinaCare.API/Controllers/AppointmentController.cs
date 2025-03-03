using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.API.Controllers;
[ApiController]
[Route("api/appointment")]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILoggerService _logger;
    private readonly IClaimsService _claimsService;

    public AppointmentController(IAppointmentService appointmentService, ILoggerService logger,
        IClaimsService claimsService)
    {
        _appointmentService = appointmentService;
        _logger = logger;
        _claimsService = claimsService;
    }

    [HttpPost("booking/single-vaccines")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<List<AppointmentDTO>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GenerateAppointments([FromBody] CreateAppointmentDto request)
    {
        try
        {
            Guid parentId = _claimsService.GetCurrentUserId;

            if (request.VaccineIds == null || !request.VaccineIds.Any())
            {
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Danh sách vaccine không hợp lệ."
                });
            }

            var result = await _appointmentService.GenerateAppointmentsForSingleVaccine(request, parentId);

            return Ok(new ApiResult<List<AppointmentDTO>>
            {
                IsSuccess = true,
                Message = "Appointments created successfully.",
                Data = result
            });
        }
        catch (ArgumentException ex)
        {
            _logger.Error($"Validation error: {ex.Message}");
            return BadRequest(new ApiResult<object>
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error in GenerateAppointments: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "Internal server error. Please try again later."
            });
        }
    }


    [HttpGet("details/{childId}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<Appointment>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAppointmentDetailsByChildId(Guid childId)
    {
        try
        {
            _logger.Info($"Received request to get appointment details for child ID: {childId}");

            var appointment = await _appointmentService.GetAppointmentDetailsByChildIdAsync(childId);

            if (appointment == null)
            {
                _logger.Warn($"No appointment found for child ID: {childId}");
                return NotFound(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "No appointment found for the specified child."
                });
            }

            _logger.Success($"Successfully retrieved appointment details for child ID: {childId}");

            return Ok(new ApiResult<Appointment>
            {
                IsSuccess = true,
                Message = "Appointment details retrieved successfully.",
                Data = appointment
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error fetching appointment details for child ID {childId}: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving the appointment details. Please try again later."
            });
        }
    }
}