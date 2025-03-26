using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.NotificationDTOs;
using VaccinaCare.Domain.Entities;

namespace VaccinaCare.API.Controllers;
[ApiController]
[Route("api/notifications")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetNotifications()
    {
        try
        {
            var notifications = await _notificationService.GetAllNotificationsByUserId();
            return Ok(ApiResult<List<NotificationResponseDTO>>.Success(notifications, "Notifications retrieved successfully."));
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, 
                ApiResult<List<NotificationResponseDTO>>.Error($"Error retrieving notifications: {e.Message}"));
        }
    }
}