using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.API.Controllers;

using VaccinaCare.Repository.Commons;

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

    //Book lịch tư vấn
    [HttpPost("consultation")]
    [Authorize(Policy = "CustomerPolicy")]
    [ProducesResponseType(typeof(ApiResult<AppointmentDTO>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> MakeConsultationAppointment([FromBody] BookConsultationDto request)
    {
        try
        {
            _logger.Info("Received request to book a consultation appointment.");

            if (request == null || request.ChildId == Guid.Empty || request.AppointmentDate == default)
            {
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid request. Please provide valid child ID and appointment date."
                });
            }

            var appointment =
                await _appointmentService.BookConsultationAppointment(request.ChildId, request.AppointmentDate);

            _logger.Success($"Consultation appointment booked successfully with ID: {appointment.Id}");

            return Ok(new ApiResult<AppointmentDTO>
            {
                IsSuccess = true,
                Message = "Consultation appointment booked successfully.",
                Data = appointment
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error while booking consultation appointment: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while booking the consultation appointment. Please try again later."
            });
        }
    }

    //User book Apppointment dựa trên Vaccines đã được tư vấn
    [HttpPost("suggestion/vaccines/{childId}")]
    [Authorize(Policy = "CustomerPolicy")]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<AppointmentDTO>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GenerateAppointmentsFromVaccineSuggestions(Guid childId, DateTime startDate)
    {
        try
        {
            _logger.Info($"Received request to generate appointments from vaccine suggestions for child ID: {childId}");

            var appointments = await _appointmentService.GenerateAppointmentsFromVaccineSuggestions(childId, startDate);

            var appointmentDTOs = appointments.Select(a => new AppointmentDTO
            {
                Id = a.Id,
                ChildId = a.ChildId,
                AppointmentDate = a.AppointmentDate,
                Status = a.Status,
                VaccineType = a.VaccineType,
                VaccineIds = a.AppointmentsVaccines.Select(av => av.VaccineId ?? Guid.Empty).ToList()
            }).ToList();

            return Ok(new ApiResult<IEnumerable<AppointmentDTO>>
            {
                IsSuccess = true,
                Message = "Appointments generated successfully.",
                Data = appointmentDTOs
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error while generating appointments: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while generating appointments. Please try again later."
            });
        }
    }


    [HttpGet("child/{childId}")]
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


    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<Pagination<CreateAppointmentDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAppointmentByParent([FromQuery] PaginationParameter pagination)
    {
        try
        {
            _logger.Info("Received request to get appointment list.");
            Guid parentId = _claimsService.GetCurrentUserId;
            var appointment = await _appointmentService.GetAppointmentByParent(parentId, pagination);

            _logger.Success($"Fetched {appointment.Count} appointment successfully.");

            return Ok(new ApiResult<Pagination<CreateAppointmentDto>>
            {
                IsSuccess = true,
                Message = "Appointment list retrieved successfully.",
                Data = appointment
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error while fetching appointment: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving the appointment list. Please try again later."
            });
        }
    }
}