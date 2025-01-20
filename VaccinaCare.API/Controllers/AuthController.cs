using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.AuthDTOs;
using VaccinaCare.Domain.DTOs.NotificationDTOs;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILoggerService _logger;
    private readonly IAuthService _authService;
    private readonly INotificationService _notificationService;

    public AuthController(ILoggerService logger, IAuthService authService, INotificationService notificationService)
    {
        _logger = logger;
        _authService = authService;
        _notificationService = notificationService;
    }


    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDTO registerDTO)
    {
        _logger.Info("Registration attempt initiated.");

        try
        {
            if (registerDTO == null || string.IsNullOrWhiteSpace(registerDTO.Email) ||
                string.IsNullOrWhiteSpace(registerDTO.Password))
            {
                _logger.Warn("Invalid registration request. Email and password are required.");
                return BadRequest(ApiResult<object>.Error("400 - Invalid registration data."));
            }

            var user = await _authService.RegisterAsync(registerDTO);

            if (user == null)
            {
                _logger.Warn($"Registration failed for email: {registerDTO.Email}. Email might already be in use.");
                return BadRequest(ApiResult<object>.Error("Registration failed. Email might already be in use."));
            }

            _logger.Success($"User {registerDTO.Email} registered successfully.");

            var notificationDTO = new NotificationDTO
            {
                Title = "Welcome to VaccinaCare!",
                Content = "Thank you for registering with VaccinaCare. We're excited to have you on board!",
                Url = "/welcome",
                UserId = user.Id
            };
            await _notificationService.PushNotificationToUser(user.Id, notificationDTO);

            return Ok(ApiResult<object>.Success(new
            {
                userId = user.Id,
                email = user.Email,
                fullName = user.FullName
            }, "Registration successful."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error during registration: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during registration."));
        }
    }
}