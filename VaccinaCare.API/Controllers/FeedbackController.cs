using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.FeedbackDTOs;
using VaccinaCare.Repository.Commons;

namespace VaccinaCare.API.Controllers;

[Route("api/feedbacks")]
[ApiController]
public class FeedbackController : ControllerBase
{
    private readonly IFeedbackService _feedbackService;
    private readonly ILoggerService _logger;

    public FeedbackController(IFeedbackService feedbackService, ILoggerService logger)
    {
        _feedbackService = feedbackService;
        _logger = logger;
    }

    /// <summary>
    ///     User tạo feedback cho appointment đã hoàn thành
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CustomerPolicy")]
    [ProducesResponseType(typeof(ApiResult<FeedbackDTO>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> CreateFeedback([FromBody] FeedbackDTO feedbackDto)
    {
        try
        {
            _logger.Info("Received request to create feedback.");

            if (feedbackDto == null || feedbackDto.AppointmentId == Guid.Empty || feedbackDto.Rating < 1 ||
                feedbackDto.Rating > 5)
                return Ok(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid feedback data. Please provide a valid appointment ID and rating between 1 and 5."
                });

            var feedback = await _feedbackService.CreateFeedbackAsync(feedbackDto);
            _logger.Success($"Feedback created successfully for Appointment {feedback.AppointmentId}.");

            return Ok(new ApiResult<FeedbackDTO>
            {
                IsSuccess = true,
                Message = "Feedback created successfully.",
                Data = feedback
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Warn($"Unauthorized access: {ex.Message}");
            return Ok(new ApiResult<object>
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.Warn($"Invalid operation: {ex.Message}");
            return Ok(new ApiResult<object>
            {
                IsSuccess = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error creating feedback: {ex.Message}");
            return Ok(new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while creating feedback. Please try again later."
            });
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResult<Pagination<FeedbackDTO>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> GetAllFeedbackPagingAsync([FromQuery] PaginationParameter pagination)
    {
        try
        {
            _logger.Info("Received request to get feedback list.");

            var feedbacks = await _feedbackService.GetAllFeedbacksAsync(pagination);

            _logger.Success($"Fetched {feedbacks.Count} feedbacks successfully.");

            return Ok(new ApiResult<object>
            {
                IsSuccess = true,
                Message = "Feedback list retrieved successfully.",
                Data = new
                {
                    Feedbacks = feedbacks,
                    totalCount = feedbacks.TotalCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error while fetching feedbacks: {ex.Message}");
            return Ok(new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving the feedback list. Please try again later."
            });
        }
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetFeedbackByUserId()
    {
        try
        {
            _logger.Info("Received request to get feedback for the current user.");

            var feedbackList = await _feedbackService.GetFeedbackByUserIdAsync();

            if (feedbackList == null || !feedbackList.Any())
            {
                _logger.Warn("No feedback found for the current user.");
                return Ok(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "No feedback found for this user."
                });
            }

            return Ok(new ApiResult<List<GetFeedbackDto>>
            {
                IsSuccess = true,
                Message = "Feedback retrieved successfully.",
                Data = feedbackList
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error retrieving feedback for the current user: {ex.Message}");
            return Ok(new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while retrieving feedback."
            });
        }
    }

    [HttpPut("{feedbackId}")]
    public async Task<IActionResult> UpdateFeedback(Guid feedbackId, [FromBody] FeedbackDTO feedbackDto)
    {
        try
        {
            _logger.Info($"Received request to update feedback {feedbackId}.");

            if (feedbackDto == null || feedbackDto.Rating < 1 || feedbackDto.Rating > 5)
                return Ok(new ApiResult<object>
                {
                    IsSuccess = false,
                    Message = "Invalid feedback data. Rating must be between 1 and 5."
                });

            var updatedFeedback = await _feedbackService.UpdateFeedbackAsync(feedbackId, feedbackDto);
            _logger.Success($"Feedback {feedbackId} updated successfully.");

            return Ok(new ApiResult<FeedbackDTO>
            {
                IsSuccess = true,
                Message = "Feedback updated successfully.",
                Data = updatedFeedback
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error updating feedback {feedbackId}: {ex.Message}");
            return Ok(new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while updating feedback. Please try again later."
            });
        }
    }

    /// <summary>
    ///     Xóa feedback (User & Admin đều có quyền xóa)
    /// </summary>
    [HttpDelete("{feedbackId}")]
    [Authorize]
    public async Task<IActionResult> DeleteFeedback(Guid feedbackId)
    {
        try
        {
            _logger.Info($"Received request to delete feedback {feedbackId}.");
            await _feedbackService.DeleteFeedbackAsync(feedbackId);
            _logger.Success($"Feedback {feedbackId} deleted successfully.");

            return Ok(new ApiResult<object>
            {
                IsSuccess = true,
                Message = "Feedback deleted successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Error deleting feedback {feedbackId}: {ex.Message}");
            return Ok(new ApiResult<object>
            {
                IsSuccess = false,
                Message = "An error occurred while deleting feedback. Please try again later."
            });
        }
    }
}