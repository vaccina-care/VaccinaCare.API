using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.VaccineDTOs;
using VaccinaCare.Domain.Entities;
using VaccinaCare.Domain.Enums;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/dashboard")]
//[Authorize(Policy = "AdminPolicy")]
public class DashboardController : Controller
{
    private readonly IAppointmentService _appointmentService;
    private readonly IChildService _childService;
    private readonly IFeedbackService _feedbackService;
    private readonly IPaymentService _paymentService;
    private readonly IVaccinePackageService _vaccinePackageService;
    private readonly IVaccineService _vaccineService;

    public DashboardController(IVaccineService vaccineService,
        IVaccinePackageService vaccinePackageService,
        IChildService childService,
        IFeedbackService feedbackService, IAppointmentService appointmentService, IPaymentService paymentService)
    {
        _vaccineService = vaccineService;
        _vaccinePackageService = vaccinePackageService;
        _childService = childService;
        _feedbackService = feedbackService;
        _appointmentService = appointmentService;
        _paymentService = paymentService;
    }


    [HttpGet("vaccines/available")]
    public async Task<IActionResult> GetAvailableVaccines()
    {
        try
        {
            var count = await _vaccineService.GetVaccineAvailable();

            return Ok(ApiResult<int>.Success(count));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error($"An error occurred: {ex.Message}"));
        }
    }

    [HttpGet("vaccines/top5-booked")]
    public async Task<IActionResult> GetTop5MostBookedVaccines()
    {
        try
        {
            var topVaccines = await _vaccineService.GetTop5MostBookedVaccinesAsync();

            if (topVaccines == null || !topVaccines.Any())
                return Ok(ApiResult<object>.Error("No vaccine bookings found."));

            return Ok(ApiResult<List<VaccineBookingDto>>.Success(topVaccines));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error($"An error occurred: {ex.Message}"));
        }
    }

    [HttpGet("vaccines/packages/most-booked")]
    public async Task<IActionResult> GetMostBookedPackage()
    {
        try
        {
            var package = await _vaccinePackageService.GetMostBookedPackageAsync();

            if (package == null) return Ok(ApiResult<object>.Error("No bookings found for any package"));

            var response = new VaccinePackage
            {
                PackageName = package.PackageName,
                Description = package.Description,
                Price = package.Price
            };

            return Ok(ApiResult<object>.Success(response));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error($"An error occurred: {ex.Message}"));
        }
    }

    [HttpGet("children/profile")]
    public async Task<IActionResult> GetChildProfile()
    {
        try
        {
            var count = await _childService.GetChildrenProfile();

            return Ok(ApiResult<int>.Success(count, "Children profile retrieved successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error($"An error occurred: {ex.Message}"));
        }
    }

    [HttpGet("feedback/overall-rating")]
    public async Task<IActionResult> GetOverallRating()
    {
        try
        {
            var (overallRating, totalFeedbackCount) = await _feedbackService.GetOverallRatingAsync();

            var response = new
            {
                AverageRating = overallRating,
                TotalRatings = totalFeedbackCount
            };

            return Ok(ApiResult<object>.Success(response, "Overall rating retrieved successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error($"An error occurred: {ex.Message}"));
        }
    }

    [HttpGet("feedback/rating-distribution")]
    public async Task<IActionResult> GetRatingDistribution()
    {
        try
        {
            var distribution = await _feedbackService.GetRatingDistributionAsync();

            return Ok(ApiResult<object>.Success(distribution, "Rating distribution retrieved successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error($"An error occurred: {ex.Message}"));
        }
    }

    [HttpGet("appointments/all")]
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
                totalCount = appointments.TotalCount, appointments
            }));
        }
        catch (Exception e)
        {
            return StatusCode(500, ApiResult<object>.Error($"An error occurred: {e.Message}"));
        }
    }

    [HttpGet("payments/amount")]
    public async Task<IActionResult> GetPaymentSummary(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] PaymentTransactionStatus? status = null)
    {
        try
        {
            var result = await _paymentService.GetPaymentTransactionSummaryAsync(startDate, endDate, status);
            return Ok(ApiResult<object>.Success(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error($"An error occurred: {ex.Message}"));
        }
    }
}