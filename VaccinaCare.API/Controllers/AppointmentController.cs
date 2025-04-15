using System.Data.Entity.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.AppointmentDTOs;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Commons;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly IClaimsService _claimsService;
    private readonly ILoggerService _logger;
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
            {
                _logger.Warn("Dữ liệu đầu vào không hợp lệ.");
                return BadRequest(ApiResult<object>.Error("Dữ liệu đầu vào không hợp lệ."));
            }

            var appointmentDTOs = await _appointmentService.GenerateAppointmentsForSingleVaccine(request, parentId);

            return Ok(ApiResult<List<AppointmentDTO>>.Success(appointmentDTOs, "Đặt lịch tiêm chủng thành công!"));
        }
        catch (DbUpdateException dbEx)
        {
            _logger.Error($"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
            return StatusCode(500, ApiResult<object>.Error("Lỗi hệ thống khi lưu lịch hẹn. Vui lòng thử lại."));
        }
        catch (ArgumentException argEx)
        {
            _logger.Warn($"Validation error: {argEx.Message}");
            return BadRequest(ApiResult<object>.Error(argEx.Message));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau."));
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
            return Ok(ApiResult<object>.Error($"An unexpected error occurred during creation: {ex.Message}"));
        }
    }

    [HttpPut("{appointmentId}/status")]
    [Authorize(Policy = "StaffPolicy")]
    [ProducesResponseType(typeof(ApiResult<AppointmentDTO>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> UpdateAppointmentStatus(Guid appointmentId,
        [FromForm] UpdateAppointmentStatusRequest request)
    {
        try
        {
            var updatedAppointment =
                await _appointmentService.UpdateAppointmentStatus(appointmentId, request.NewStatus,
                    request.CancellationReason);

            if (updatedAppointment == null)
                return Ok(ApiResult<object>.Error(
                    "Unable to update appointment status. Please check the validity conditions."));

            return Ok(ApiResult<AppointmentDTO>.Success(updatedAppointment,
                "Appointment status updated successfully."));
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<object>.Error(ex.Message));
        }
    }

    [HttpGet]
    [Authorize(Policy = "StaffPolicy")]
    [ProducesResponseType(typeof(ApiResult<Pagination<AppointmentDTO>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAllAppointments(
        [FromQuery] PaginationParameter pagination,
        [FromQuery] string? searchTerm = null,
        [FromQuery] AppointmentStatus? status = null)
    {
        try
        {
            var appointments = await _appointmentService.GetAllAppointments(pagination, searchTerm, status);

            return Ok(ApiResult<object>.Success(new
            {
                totalCount = appointments.TotalCount,
                appointments = appointments
            }));
        }
        catch (Exception e)
        {
            return StatusCode(500, ApiResult<object>.Error($"An error occurred: {e.Message}"));
        }
    }

    [HttpGet("{childId}")]
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
                return BadRequest(ApiResult<object>.Error("Không có lịch hẹn nào cho trẻ này."));

            return Ok(ApiResult<List<AppointmentDTO>>.Success(appointments, "Lấy danh sách lịch hẹn thành công."));
        }
        catch (Exception e)
        {
            return StatusCode(500, ApiResult<object>.Error($"An error occurred: {e.Message}"));
        }
    }

    [HttpGet("details/{appointmentId}")]
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