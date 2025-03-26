using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/dashboard")]
//[Authorize(Policy = "AdminPolicy")]
public class DashboardController : Controller
{
    private readonly IVaccinePackageService _vaccinePackageService;
    private readonly IVaccineService _vaccineService;
    private readonly IChildService _childService;
    private readonly IFeedbackService _feedbackService;
    public DashboardController(IVaccineService vaccineService,
        IVaccinePackageService vaccinePackageService,
        IChildService childService,
        IFeedbackService feedbackService)
    {
        _vaccineService = vaccineService;
        _vaccinePackageService = vaccinePackageService;
        _childService = childService;
        _feedbackService = feedbackService;
    }

    [HttpGet("vaccines/available")]
    public async Task<IActionResult> GetAvailableVaccines()
    {
        try
        {
            var count = await _vaccineService.GetVaccineAvailable();

            return Ok(ApiResult<int>.Success(count, "Available vaccines retrieved successfully."));
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<int>.Error($"An error occurred: {ex.Message}"));
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
            return Ok(ApiResult<int>.Error($"An error occurred: {ex.Message}"));
        }
    }
    [HttpGet("childs/profile")]
    public async Task<IActionResult> GetChildProfile()
    {
        try
        {
            var count = await _childService.GetChildrenProfile();

            return Ok(ApiResult<int>.Success(count, "Children profile retrieved successfully."));
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<int>.Error($"An error occurred: {ex.Message}"));
        }
    }
    [HttpGet("feedback/overallrating")]
    public async Task<IActionResult> GetOverallRating()
    {
        try
        {
            double overallRating = await _feedbackService.GetOverallRatingAsync();

            return Ok(ApiResult<double>.Success(overallRating, "Overall rating retrieved successfully."));
        }
        catch (Exception ex)
        {
            return Ok(ApiResult<double>.Error($"An error occurred : {ex.Message}"));
        }
    }
}