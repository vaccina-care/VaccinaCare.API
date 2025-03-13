using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Interface.Common;
using VaccinaCare.Application.Ultils;
using VaccinaCare.Domain.DTOs.AuthDTOs;
using VaccinaCare.Domain.DTOs.EmailDTOs;
using VaccinaCare.Domain.DTOs.NotificationDTOs;
using VaccinaCare.Repository.Interfaces;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ILoggerService _logger;
    private readonly IAuthService _authService;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;
    private readonly IClaimsService _claimsService;


    public AuthController(ILoggerService logger, IAuthService authService, INotificationService notificationService,
        IEmailService emailService, IClaimsService claimsService)
    {
        _logger = logger;
        _authService = authService;
        _notificationService = notificationService;
        _emailService = emailService;
        _claimsService = claimsService;
    }


    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDTO registerDTO)
    {
        if (registerDTO == null || string.IsNullOrWhiteSpace(registerDTO.Email) ||
            string.IsNullOrWhiteSpace(registerDTO.Password))
            return BadRequest(ApiResult<object>.Error("400 - Invalid registration data."));

        try
        {
            var user = await _authService.RegisterAsync(registerDTO);

            if (user == null)
                return BadRequest(ApiResult<object>.Error("Registration failed. Email might already be in use."));

            var response = new RegisterRequestDTO
            {
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = registerDTO.PhoneNumber,
                Gender = registerDTO.Gender,
                DateOfBirth = registerDTO.DateOfBirth,
                ImageUrl = registerDTO.ImageUrl
            };

            return Ok(ApiResult<RegisterRequestDTO>.Success(response, "Registration successful."));
        }
        catch (Exception ex)
        {
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
            if (loginDTO == null || string.IsNullOrWhiteSpace(loginDTO.Email) ||
                string.IsNullOrWhiteSpace(loginDTO.Password))
            {
                _logger.Warn("Invalid login request. Email and password are required.");
                return BadRequest(ApiResult<object>.Error("400 - Invalid login data."));
            }

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true)
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

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var currentUserId = _claimsService.GetCurrentUserId;

            _logger.Info($"Logout request initiated for user ID: {currentUserId}");

            var result = await _authService.LogoutAsync(currentUserId);
            if (!result)
            {
                _logger.Warn($"Logout failed for user ID: {currentUserId}. User might not exist.");
                return BadRequest(ApiResult<object>.Error("Logout failed. User might not exist."));
            }

            _logger.Success($"User {currentUserId} logged out successfully.");
            return Ok(ApiResult<object>.Success(null, "Logout successful."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error during logout: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during logout."));
        }
    }

    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(ApiResult<LoginResponseDTO>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 401)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> RefreshToken([FromBody] TokenRefreshRequestDTO tokenRefreshRequestDto)
    {
        _logger.Info("Token refresh attempt initiated.");

        try
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, false)
                .AddEnvironmentVariables()
                .Build();

            var response = await _authService.RefreshTokenAsync(tokenRefreshRequestDto, configuration);

            if (response == null) return Unauthorized(ApiResult<object>.Error("Invalid or expired refresh token."));

            _logger.Info("Token refresh successful.");
            return Ok(ApiResult<LoginResponseDTO>.Success(response, "Token refreshed successfully."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Error during token refresh: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("Internal server error."));
        }
    }
}