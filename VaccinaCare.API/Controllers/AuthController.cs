using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.AuthDTOs;
using VaccinaCare.Domain.DTOs.NotificationDTOs;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ILoggerService _logger;
    private readonly IAuthService _authService;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;

    public AuthController(ILoggerService logger, IAuthService authService, INotificationService notificationService, IEmailService emailService)
    {
        _logger = logger;
        _authService = authService;
        _notificationService = notificationService;
        _emailService = emailService;
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
            await _emailService.SendWelcomeNewUserAsync(user.Email, user.FullName);

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


    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto loginDTO)
    {
        _logger.Info($"Login attempt initiated for user: {loginDTO.Email}");

        try
        {
            if (loginDTO == null || string.IsNullOrWhiteSpace(loginDTO.Email) || string.IsNullOrWhiteSpace(loginDTO.Password))
            {
                _logger.Warn("Invalid login request. Email and password are required.");
                return BadRequest(ApiResult<object>.Error("400 - Invalid login data."));
            }

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            var loginResponse = await _authService.LoginAsync(loginDTO, configuration);

            if (loginResponse == null)
            {
                _logger.Warn($"Login failed for user: {loginDTO.Email}");
                return Unauthorized(ApiResult<object>.Error("Invalid email or password."));
            }

            _logger.Success($"User {loginDTO.Email} logged in successfully.");
            return Ok(ApiResult<object>.Success(new
            {
                accessToken = loginResponse.AccessToken,
                refreshToken = loginResponse.RefreshToken
            }, "Login successful."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error during login: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during login."));
        }
    }

}