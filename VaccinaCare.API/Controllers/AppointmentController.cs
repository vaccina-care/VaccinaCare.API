using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILoggerService _logger;
    private readonly IClaimsService _claimsService;
    private readonly IPaymentService _paymentService;

    public AppointmentController(IAppointmentService appointmentService, ILoggerService logger,
        IClaimsService claimsService, IPaymentService paymentService)
    {
        _appointmentService = appointmentService;
        _logger = logger;
        _claimsService = claimsService;
        _paymentService = paymentService;
    }

    [HttpPost("booking/single-vaccines")]
    [ProducesResponseType(typeof(ApiResult<List<AppointmentDTO>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> BookAppointmentsForSingleVaccine(
        [FromBody] CreateAppointmentSingleVaccineDto request)
    {
        try
        {
            var parentId = _claimsService.GetCurrentUserId;
            if (request == null || request.VaccineId == Guid.Empty || request.ChildId == Guid.Empty)
                return Ok(ApiResult<object>.Error("Dữ liệu đầu vào không hợp lệ."));

            var appointmentDTOs = await _appointmentService.GenerateAppointmentsForSingleVaccine(request, parentId);

            return Ok(ApiResult<List<AppointmentDTO>>.Success(appointmentDTOs, "Đặt lịch tiêm chủng thành công!"));
        }
        catch (ArgumentException ex)
        {
            return Ok(ApiResult<object>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<object>.Error("Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau."));
        }
    }

    [HttpPost("booking/package-vaccines")]
    [ProducesResponseType(typeof(ApiResult<List<AppointmentDTO>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> BookAppointmentsForPackageVaccine(
        [FromBody] CreateAppointmentPackageVaccineDto request)
    {
        try
        {
            var parentId = _claimsService.GetCurrentUserId;
            if (request == null || request.PackageId == Guid.Empty || request.ChildId == Guid.Empty)
                return BadRequest(ApiResult<object>.Error("Dữ liệu đầu vào không hợp lệ."));

            var appointments = await _appointmentService.GenerateAppointmentsForPackageVaccine(request, parentId);

            return Ok(ApiResult<List<AppointmentDTO>>.Success(appointments, "Lịch hẹn đã được tạo thành công."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResult<object>.Error(ex.Message));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau."));
        }
    }

    [HttpPost("booking/consultant")]
    [ProducesResponseType(typeof(ApiResult<List<AppointmentDTO>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> BookAppointmentsForConsultant()
    {
        return null;
    }

    [HttpPut("{appointmentId}/date")]
    [ProducesResponseType(typeof(ApiResult<List<AppointmentDTO>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> UpdateAppointmentDate(Guid appointmentId, DateTime newDate)
    {
        try
        {
            var updatedAppointments = await _appointmentService.UpdateAppointmentDate(appointmentId, newDate);

            if (updatedAppointments == null)
                return Ok(ApiResult<object>.Error("Không thể cập nhật ngày tiêm. Vui lòng kiểm tra điều kiện hợp lệ."));
            return Ok(ApiResult<List<AppointmentDTO>>.Success(updatedAppointments, "Cập nhật ngày tiêm thành công."));
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<object>.Error($"An unexpected error occurred during creation: {ex}"));
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<List<AppointmentDTO>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAppointmentsByChildId(Guid childId)
    {
        try
        {
            if (childId == Guid.Empty) return Ok(ApiResult<object>.Error("Child ID không hợp lệ."));

            var appointments = await _appointmentService.GetListlAppointmentsByChildIdAsync(childId);

            if (appointments == null || !appointments.Any())
                return Ok(ApiResult<object>.Error("Không có lịch hẹn nào cho trẻ này."));

            return Ok(ApiResult<List<AppointmentDTO>>.Success(appointments, "Lấy danh sách lịch hẹn thành công."));
        }
        catch (Exception e)
        {
            return Ok(ApiResult<object>.Error("Lỗi hệ thống. Vui lòng thử lại sau."));
        }
    }

    [HttpGet("{appointmentId}")]
    [ProducesResponseType(typeof(ApiResult<AppointmentDTO>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAppointmentDetailsByChildId(Guid appointmentId)
    {
        try
        {
            var appointment = await _appointmentService.GetAppointmentDetailsByIdAsync(appointmentId);

            if (appointment == null)
                return NotFound(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "No appointment found for the specified child."
                });

            return Ok(new ApiResult<AppointmentDTO>
            {
                IsSuccess = true,
                Message = "Appointment details retrieved successfully.",
                Data = appointment
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving the appointment details. Please try again later."
            });
        }
    }
}