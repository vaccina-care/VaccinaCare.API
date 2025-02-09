using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
namespace VaccinaCare.API.Controllers;
using VaccinaCare.Repository.Commons;

[ApiController]
    [Route("api/appointment")]

    public class AppointmentController : ControllerBase
    {
      private readonly IAppointmentService _appointmentService;
      private readonly ILoggerService _logger;
    public AppointmentController(IAppointmentService appointmentService, ILoggerService logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<CreateAppointmentDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> CreateAppointment([FromBody] CreateAppointmentDto createAppointmentDto)
    {
        try
        {
            _logger.Info("Received request to create appointment.");

            if (createAppointmentDto == null)
            {
                _logger.Warn("CreateAppointmentDTO is null.");
                return BadRequest(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid data. Appointment is required."
                });

            }
            var appointment = await _appointmentService.CreateAppointment(createAppointmentDto);

            _logger.Success($"Appointment information created successfully.");

            return Ok(new ApiResult<CreateAppointmentDto>
            {
                IsSuccess = true,
                Message = "Appointment information created successfully.",
                Data = appointment
            });
        }
        catch (Exception ex) 
        {
            _logger.Error($"Error while creating appointment information: {ex.Message}");
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while creating the appointment information. Please try again later."
            });
        }
    }
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<Pagination<CreateAppointmentDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAppointmentByParent([FromQuery] PaginationParameter paginatio)
    {
        try
        {
            _logger.Info("Received request to get appointment list.");

            var appointment = await _appointmentService.GetAppointmentByParent(paginatio);

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

