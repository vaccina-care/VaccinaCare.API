using Microsoft.AspNetCore.Mvc;
using VaccinaCare.Application.Interface;
using VaccinaCare.Application.Ultils;
using Microsoft.AspNetCore.Authorization;
using VaccinaCare.Domain.DTOs.UserDTOs;
using VaccinaCare.Domain.DTOs.AuthDTOs;
using VaccinaCare.Domain.DTOs.EmailDTOs;
using VaccinaCare.Domain.DTOs.NotificationDTOs;
using VaccinaCare.Application.Interface.Common;

namespace VaccinaCare.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminPolicy")]
public class AdminController : Controller
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly ILoggerService _logger;

    public AdminController(IUserService userService, IAuthService authService, ILoggerService loggerService)
    {
        _userService = userService;
        _authService = authService;
        _logger = loggerService;
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _userService.GetAllUsersForAdminAsync();

            return Ok(new ApiResult<object>
            {
                IsSuccess = true,
                Data = users,
                Message = "Fetched all users successfully."
            });
        }
        catch (Exception ex)
        {
            // Log and return error response
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Data = null,
                Message = $"An error occurred: {ex.Message}"
            });
        }
    }

    [HttpPost("users/staff")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDto createStaffDto)
    {

        if (createStaffDto == null || string.IsNullOrWhiteSpace(createStaffDto.Email) ||
            string.IsNullOrWhiteSpace(createStaffDto.Password))
        {
            return BadRequest(ApiResult<object>.Error("400 - Invalid registration data."));
        }

        try
        {
            var user = await _userService.CreateStaffAsync(createStaffDto);

            if (user == null)
            {
                return BadRequest(ApiResult<object>.Error("Creating failed. Email might already be in use."));
            }

            var staff = new CreateStaffDto
            {
                Email = user.Email,
                FullName = user.FullName
            };

            return Ok(ApiResult<CreateStaffDto>.Success(staff, "Registration successful."));
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error during creating staff: {ex.Message}");
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during creating staff."));
        }
    }

    [HttpPut("users/{userId}")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    public async Task<IActionResult> UpdateUserInfo(Guid userId, [FromBody] UserUpdateDto userUpdateDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new ApiResult<object>
            {
                IsSuccess = false,
                Message = "Invalid request data",
                Data = ModelState
            });

        try
        {
            var updatedUser = await _userService.UpdateUserInfo(userId, userUpdateDto);

            return Ok(new ApiResult<object>
            {
                IsSuccess = true,
                Message = "User info updated successfully",
                Data = updatedUser
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ApiResult<object>
            {
                IsSuccess = false,
                Message = "User not found"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResult<object>
            {
                IsSuccess = false,
                Message = "Internal server error"
            });
        }
    }

    [HttpDelete("users/{userId}")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 500)]
    public async Task<IActionResult> DeleteUser(Guid userId)
    {

        if (userId == Guid.Empty)
        {
            return BadRequest(ApiResult<object>.Error("400 - Invalid delete request. User ID is required."));
        }
        try
        {
            var result = await _userService.DeactivateUserAsync(userId);

            if (!result)
            {
                return BadRequest(ApiResult<object>.Error("Delete failed. No user found with the provided ID."));
            }

            return Ok(ApiResult<object>.Success(null, "User deleted successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<object>.Error("An unexpected error occurred during deleting user."));
        }
    }
}